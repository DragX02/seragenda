# Configuration OAuth pour Google et Microsoft

## Configuration Google OAuth

1. **Créer un projet Google Cloud**
   - Allez sur https://console.cloud.google.com/
   - Créez un nouveau projet ou sélectionnez un projet existant

2. **Activer l'API Google+**
   - Dans le menu, allez à "APIs & Services" > "Library"
   - Recherchez "Google+ API" et activez-la

3. **Créer des identifiants OAuth**
   - Allez à "APIs & Services" > "Credentials"
   - Cliquez sur "Create Credentials" > "OAuth client ID"
   - Type d'application: "Web application"
   - Nom: "AgendaProf"
   - Authorized redirect URIs:
     - `http://192.168.1.2:5276/api/auth/google-callback`
     - `http://localhost:5276/api/auth/google-callback`

4. **Copier les identifiants**
   - Copiez le **Client ID** et le **Client Secret**
   - Mettez-les dans `appsettings.json`:
     ```json
     "OAuth": {
       "Google": {
         "ClientId": "VOTRE_CLIENT_ID_ICI",
         "ClientSecret": "VOTRE_CLIENT_SECRET_ICI",
         "RedirectUri": "http://192.168.1.2:5276/api/auth/google-callback"
       }
     }
     ```

---

## Configuration Microsoft OAuth

1. **Créer une application Azure AD**
   - Allez sur https://portal.azure.com/
   - Recherchez "Azure Active Directory"
   - Allez à "App registrations" > "New registration"

2. **Enregistrer l'application**
   - Nom: "AgendaProf"
   - Supported account types: "Accounts in any organizational directory and personal Microsoft accounts"
   - Redirect URI:
     - Platform: "Web"
     - URI: `http://192.168.1.2:5276/api/auth/microsoft-callback`

3. **Créer un Client Secret**
   - Dans votre application, allez à "Certificates & secrets"
   - Cliquez sur "New client secret"
   - Description: "AgendaProf Secret"
   - Expiration: choisissez la durée souhaitée
   - **Important**: Copiez immédiatement la valeur du secret (elle ne sera plus visible après)

4. **Configurer les permissions**
   - Allez à "API permissions"
   - Cliquez sur "Add a permission" > "Microsoft Graph"
   - Sélectionnez "Delegated permissions"
   - Ajoutez:
     - `openid`
     - `email`
     - `profile`

5. **Copier les identifiants**
   - Le **Client ID** (Application ID) se trouve sur la page "Overview"
   - Le **Client Secret** est celui que vous avez créé
   - Mettez-les dans `appsettings.json`:
     ```json
     "OAuth": {
       "Microsoft": {
         "ClientId": "VOTRE_CLIENT_ID_ICI",
         "ClientSecret": "VOTRE_CLIENT_SECRET_ICI",
         "RedirectUri": "http://192.168.1.2:5276/api/auth/microsoft-callback"
       }
     }
     ```

---

## Tester l'authentification

1. **Redémarrer le serveur**
   ```bash
   cd G:\csharp\serappagenda\seragenda\seragenda
   dotnet run
   ```

2. **Tester dans l'application**
   - Lancez l'application MAUI
   - Cliquez sur "Se connecter avec Google" ou "Se connecter avec Microsoft"
   - Une fenêtre du navigateur devrait s'ouvrir
   - Connectez-vous avec votre compte
   - Vous serez redirigé vers l'application avec un token

## Résolution des problèmes

- **Erreur "redirect_uri_mismatch"**: Vérifiez que les URIs de redirection correspondent exactement dans la console Google/Azure et dans `appsettings.json`
- **Erreur "invalid_client"**: Vérifiez que le Client ID et Client Secret sont corrects
- **Erreur CORS**: Si vous testez depuis un navigateur web, ajoutez la configuration CORS dans le serveur
