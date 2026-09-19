using System.Net.Http.Headers;
using System.Text.Json;

namespace AzureAiSamples.vision;

/// <summary>
/// Sample didattico: connessione ad Azure AI Video Indexer tramite API REST.
/// 
/// Dimostra l'intero flusso di analisi video:
///   1. Ottenere un Access Token (GET /Auth/.../AccessToken)
///   2. Caricare il video in multipart/form-data (POST /.../Videos)
///   3. Attendere il completamento dell'indicizzazione (polling GET /.../Index)
///   4. Recuperare e mostrare gli insights (GET /.../Index completo)
/// 
/// PREREQUISITI:
///   - Account Azure AI Video Indexer (standard)
///   - API Key  dal portale: https://api-portal.videoindexer.ai/profile  → sezione "Subscriptions"
///   - Account ID: visibile nell'Overview della risorsa su Azure Portal (GUID)
///   - Location:   nome della region (es. "trial", "westus2", "eastus", "westeurope")
/// 
/// OUTPUT DI ESEMPIO (JSON semplificato):
/// {
///   "id": "abc123def",
///   "name": "mio_video.mp4",
///   "state": "Processed",
///   "durationInSeconds": 120,
///   "summarizedInsights": {
///     "keywords":       [ {"keyword": "azure"},  {"keyword": "cloud"} ],
///     "sentiments":     [ {"sentimentType": "Positive", "appearances": 5} ],
///     "transcript":     [ {"text": "Benvenuti...", "confidence": 0.98} ],
///     "topics":         [ {"name": "Tecnologia"}, {"name": "Cloud"} ],
///     "labels":         [ {"name": "computer"},   {"name": "schermo"} ],
///     "namedPeople":    [ {"name": "Mario Rossi"} ],
///     "ocr":            [ {"line": "Azure Conference 2026"} ],
///     "scenes":         [ {"id": 1, "instances": [{"start": "0:00:00", "end": "0:00:30"}]} ],
///     "faces":          [ {"id": 1, "name": "Unknown"} ],
///     "audioEffects":   [ {"type": "Silence", "instances": [...]} ]
///   },
///   "videos": [{ "insights": { ... } }]
/// }
/// 
/// DOCUMENTAZIONE UFFICIALE:
///   https://learn.microsoft.com/azure/azure-video-indexer/video-indexer-use-apis
///   https://api-portal.videoindexer.ai/
/// </summary>
internal class VideoIndexerSample
{
    // ================================================================
    // CAMPI PRIVATI
    // ================================================================

    /// <summary>
    /// API Key (subscription key) ottenuta dal portale sviluppatori Video Indexer:
    /// https://api-portal.videoindexer.ai/profile → Subscriptions → Primary Key
    /// </summary>
    private readonly string _apiKey;

    /// <summary>
    /// GUID dell'account Video Indexer (visibile nell'Overview della risorsa su Azure Portal).
    /// </summary>
    private readonly string _accountId;

    /// <summary>
    /// Location (regione) dell'account Video Indexer.
    /// Per account trial usare "trial", altrimenti la region Azure (es. "westus2", "eastus").
    /// </summary>
    private readonly string _location;

    /// <summary>
    /// HttpClient condiviso (best practice .NET: riusare la stessa istanza).
    /// Il base address punta sempre a https://api.videoindexer.ai
    /// </summary>
    private readonly HttpClient _httpClient;

    // ================================================================
    // COSTRUTTORE
    // ================================================================

    /// <summary>
    /// Inizializza il client per Video Indexer con le credenziali necessarie.
    /// </summary>
    /// <param name="apiKey">
    ///   Chiave di sottoscrizione (Primary Key) dal portale API Video Indexer.
    ///   Si trova su https://api-portal.videoindexer.ai/profile
    /// </param>
    /// <param name="accountId">
    ///   GUID dell'account Video Indexer.
    ///   Si recupera dal portale Azure → risorsa Video Indexer → Overview → Account ID.
    /// </param>
    /// <param name="location">
    ///   Location/region dell'account. Per trial usare "trial",
    ///   altrimenti la region Azure (es. "westus2", "eastus", "westeurope").
    ///   Elenco completo: https://learn.microsoft.com/azure/azure-video-indexer/regions
    /// </param>
    public VideoIndexerSample(string apiKey, string accountId, string location)
    {
        _apiKey = apiKey;
        _accountId = accountId;
        _location = location;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.videoindexer.ai"),
            Timeout = TimeSpan.FromMinutes(10) // upload + indexing può richiedere tempo
        };
    }
// ================================================================
    // METODO PRINCIPALE: ANALIZZA VIDEO
    // ================================================================

    /// <summary>
    /// Carica un video su Azure AI Video Indexer, avvia l'indicizzazione e
    /// restituisce il JSON completo degli insights estratti.
    /// 
    /// FLUSSO DETTAGLIATO:
    /// 
    ///  STEP 1 — Access Token
    ///    GET /Auth/{location}/Accounts/{accountId}/AccessToken?allowEdit=true
    ///    Header: Ocp-Apim-Subscription-Key: {apiKey}
    ///    Response: stringa JWT (token di accesso della durata di 1 ora)
    /// 
    ///  STEP 2 — Upload Video
    ///    POST /{location}/Accounts/{accountId}/Videos?name={fileName}&amp;privacy=Private&amp;language=it-IT
    ///    Header: Authorization: Bearer {accessToken}
    ///    Body:   multipart/form-data con il file video
    ///    Response: JSON con "id" (videoId) e "state" = "Uploaded"
    /// 
    ///  STEP 3 — Polling (attesa indicizzazione)
    ///    GET /{location}/Accounts/{accountId}/Videos/{videoId}/Index
    ///    Header: Authorization: Bearer {accessToken}
    ///    Finchè lo stato non è "Processed", aspettiamo e riproviamo
    /// 
    ///  STEP 4 — Recupero Insights
    ///    GET /{location}/Accounts/{accountId}/Videos/{videoId}/Index?language=it-IT
    ///    Header: Authorization: Bearer {accessToken}
    ///    Response: JSON completo con tutti gli insights
    /// 
    /// </summary>
    /// <param name="videoPath">
    ///   Percorso del file video da analizzare.
    ///   Formati supportati: mp4, mov, wmv, avi, mkv, mp3, wav, ecc.
    ///   Dimensione max (upload diretto): 2 GB.
    /// </param>
    /// <returns>
    ///   Stringa JSON formattata con tutti gli insights estratti da Video Indexer.
    /// </returns>
    public async Task<string> AnalyzeVideoAsync(string videoPath)
    {
        if (!File.Exists(videoPath))
            throw new FileNotFoundException($"File video non trovato: {videoPath}", videoPath);

        string videoFileName = Path.GetFileName(videoPath);
        Console.WriteLine($"📁 Video: {videoFileName}");
        Console.WriteLine($"🌍 Location: {_location}");
        Console.WriteLine($"🆔 Account: {_accountId}\n");

        // --- STEP 1: Access Token ---
        Console.WriteLine("🔑 STEP 1: Richiesta Access Token...");
        string tokenUrl = $"/Auth/{_location}/Accounts/{_accountId}/AccessToken?allowEdit=true";
        using var tokenReq = new HttpRequestMessage(HttpMethod.Get, tokenUrl);
        tokenReq.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);
        var tokenResp = await _httpClient.SendAsync(tokenReq);
        tokenResp.EnsureSuccessStatusCode();
        string accessToken = (await tokenResp.Content.ReadAsStringAsync()).Trim('"');
        Console.WriteLine("   ✅ Access Token ottenuto.\n");

        // --- STEP 2: Upload Video ---
        Console.WriteLine("📤 STEP 2: Upload del video in corso...");
        string uploadUrl = $"/{_location}/Accounts/{_accountId}/Videos" +
                           $"?name={Uri.EscapeDataString(videoFileName)}" +
                           "&privacy=Private&language=it-IT";
        using var multipart = new MultipartFormDataContent();
        var fileStream = new FileStream(videoPath, FileMode.Open, FileAccess.Read);
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        multipart.Add(streamContent, "video", videoFileName);

        using var uploadReq = new HttpRequestMessage(HttpMethod.Post, uploadUrl)
        {
            Content = multipart
        };
        uploadReq.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var uploadResp = await _httpClient.SendAsync(uploadReq);
        string uploadBody = await uploadResp.Content.ReadAsStringAsync();

        if (!uploadResp.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"Upload fallito ({(int)uploadResp.StatusCode}): {uploadBody}");

        using JsonDocument uploadJson = JsonDocument.Parse(uploadBody);
        string videoId = uploadJson.RootElement.GetProperty("id").GetString()!;

        Console.WriteLine($"   ✅ Upload completato. Video ID: {videoId}\n");

        // --- STEP 3: Polling fino a indicizzazione completata ---
        Console.WriteLine("⏳ STEP 3: Attesa indicizzazione (polling)...");
        string indexUrl = $"/{_location}/Accounts/{_accountId}/Videos/{videoId}/Index";
        string videoState = "";
        int attempt = 0;
        const int maxAttempts = 60;
        const int delayMs = 5000;

        do
        {
            attempt++;
            await Task.Delay(delayMs);
            using var pollReq = new HttpRequestMessage(HttpMethod.Get, indexUrl);
            pollReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var pollResp = await _httpClient.SendAsync(pollReq);

            if (pollResp.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine($"   ⏱️  Tentativo {attempt}: non ancora disponibile...");
                videoState = "NotFound";
            }
            else
            {
                string pollBody = await pollResp.Content.ReadAsStringAsync();
                if (pollBody.Contains("\"ErrorType\""))
                {
                    Console.WriteLine($"   ⏱️  Tentativo {attempt}: ancora in elaborazione...");
                    videoState = "Processing";
                }
                else
                {
                    using JsonDocument pollJson = JsonDocument.Parse(pollBody);
                    videoState = "Processed";
                    if (pollJson.RootElement.TryGetProperty("state", out JsonElement st))
                        videoState = st.GetString()!;
                    else if (pollJson.RootElement.TryGetProperty("videos", out JsonElement ve) &&
                             ve.GetArrayLength() > 0 &&
                             ve[0].TryGetProperty("state", out JsonElement vs))
                        videoState = vs.GetString()!;
                    Console.WriteLine($"   📊 Tentativo {attempt}: stato = {videoState}");
                }
            }

            if (attempt >= maxAttempts)
                throw new TimeoutException($"Indicizzazione non completata dopo {maxAttempts} tentativi.");

        } while (videoState != "Processed");

        Console.WriteLine("   ✅ Indicizzazione completata!\n");

        // --- STEP 4: Recupero Insights ---
        Console.WriteLine("📊 STEP 4: Recupero insights...");
        string fullIndexUrl = $"/{_location}/Accounts/{_accountId}/Videos/{videoId}/Index?language=it-IT";
        using var indexReq = new HttpRequestMessage(HttpMethod.Get, fullIndexUrl);
        indexReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var indexResp = await _httpClient.SendAsync(indexReq);
        indexResp.EnsureSuccessStatusCode();
        string rawJson = await indexResp.Content.ReadAsStringAsync();
        using JsonDocument fullJson = JsonDocument.Parse(rawJson);
        string formattedJson = JsonSerializer.Serialize(
            fullJson.RootElement,
            new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine("   ✅ Insights recuperati!\n");

        fileStream.Close();
        return formattedJson;
    }
}