# Le mie Ricette

App mobile di ricette: backend .NET 9 + SQLite, frontend Angular 20 + Capacitor (Android/iOS),
estrazione ricette con IA (llama.cpp OpenAI-compatible) da link o testo, con scalatura automatica delle dosi per numero di persone.

## Struttura

```
proj/
  backend/    API .NET 9 (EF Core + SQLite) su http://localhost:5197
  frontend/   Angular 20 + Capacitor (webDir: dist/recipe-app/browser)
```

## Backend

```powershell
cd proj\backend
dotnet run --urls http://localhost:5197
```

Il database `recipes.db` viene creato automaticamente. Le foto si salvano in `uploads/`
e sono servite su `/uploads/...`.

### Endpoint

| Metodo | Path | Descrizione |
|--------|------|-------------|
| GET | `/api/recipes?search=` | Lista ricette (ricerca per titolo/categoria) |
| GET | `/api/recipes/{id}` | Dettaglio ricetta |
| POST | `/api/recipes` | Crea ricetta |
| PUT | `/api/recipes/{id}` | Aggiorna ricetta |
| DELETE | `/api/recipes/{id}` | Elimina ricetta (e foto) |
| POST | `/api/recipes/{id}/photo` | Upload foto (multipart, campo `file`, max 15 MB) |
| POST | `/api/extract` | Estrazione IA da `{ url }` o `{ text }` |

## Configurazione IA (llama.cpp)

L'IA parla con un server **llama.cpp** (llama-server) che espone l'API
OpenAI-compatible `/chat/completions`. Il server gira su un host dedicato nella
sottorete: compila i valori in `proj\backend\appsettings.json`:

```json
"Ai": {
  "BaseUrl": "http://<IP-del-server>:<PORTA>",
  "Model": "nome-del-modello",
  "ApiKey": "",
  "MaxTokens": 1024,
  "TimeoutSeconds": 180
}
```

- `BaseUrl`: es. `http://192.168.1.50:8080` (l'API viene chiamata su `{BaseUrl}/chat/completions`).
- `Model`: opzionale, se il server serve un solo modello.
- `ApiKey`: se il `llama-server` è avviato con `--api-key`.
- Se `BaseUrl` è vuoto l'IA è disabilitata: `/api/extract` risponde con un
  messaggio e una ricetta vuota da compilare a mano.

Lanci tipico del server dedicato:

```bash
llama-server -m modello.gguf --host 0.0.0.0 --port 8080 [--api-key XXXX]
```

## Frontend

```powershell
cd proj\frontend
npm install

# sviluppo web
npm start                      # http://localhost:4200

# build + sincronizza Capacitor
npm run cap:sync

# apre Android Studio / Xcode
npm run cap:open:android
npm run cap:open:ios
```

L'app punta di default a `http://10.0.2.2:5197` (host PC visto dall'emulatore
Android). Su un **dispositivo reale** modifica `baseUrl` in
`src\app\api.ts` con l'IP del PC nella rete (es. `http://192.168.1.10:5197/api`).
Il `AndroidManifest.xml` ha già `usesCleartextTraffic="true"` per l'HTTP in LAN.

### Schermate

- **Ricette** — lista con ricerca, tap per dettaglio.
- **Dettaglio** — foto (upload), badge, **porzioni con +/- che riscalano le dosi**,
  ingredienti e procedimento numerati, modifica/elimina.
- **Nuova/Modifica** — form completo con ingredienti (quantità + unità) e passi.
- **Importa (IA)** — incolla un link o il testo di una ricetta: l'IA estrae i campi
  e l'app mostra un'anteprima salvabile in un click.
