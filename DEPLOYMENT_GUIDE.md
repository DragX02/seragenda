# Guide de Déploiement Sécurisé - SerAgenda API

## 🔒 Configuration Sécurisée

### Structure des fichiers de configuration

- **`appsettings.json`** : Configuration par défaut (sans secrets) - **COMMITÉ dans Git**
- **`appsettings.Development.json`** : Configuration locale - **EXCLU de Git** (`.gitignore`)
- **`appsettings.Production.json`** : Configuration serveur - **EXCLU de Git** (`.gitignore`)

### ⚠️ IMPORTANT : Première mise en place sur le serveur

Avant le premier déploiement, créez manuellement le fichier sur votre serveur :

```bash
# Sur votre serveur (192.168.1.79)
sudo nano /var/www/serapi/appsettings.Production.json
```

Contenu du fichier :
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=192.168.1.79;Port=5497;Database=db_agenda;Username=admin_api01;Password=Aspirees15"
  },
  "JwtSettings": {
    "SecretKey": "oZiFAswBjPgRk-Kj3nXwQ8kxLI5mAugOVYU6k73rcsw="
  },
  "ApiSettings": {
    "BaseUrl": "http://192.168.1.79:5276/api/"
  }
}
```

Ensuite, protégez le fichier :
```bash
sudo chmod 600 /var/www/serapi/appsettings.Production.json
sudo chown www-data:www-data /var/www/serapi/appsettings.Production.json
```

---

## 📱 Configuration BaseUrl pour votre Application Client

Si vous avez une application mobile ou frontend, utilisez la compilation conditionnelle :

### Option 1 : Classe de Configuration C# (recommandé)

Créez `Constants.cs` dans votre projet client :

```csharp
namespace VotreAppClient
{
    public static class ApiConfig
    {
#if DEBUG
        public const string BaseUrl = "http://localhost:5276/api/";
#else
        public const string BaseUrl = "http://192.168.1.79:5276/api/";
#endif
    }
}
```

Utilisation :
```csharp
var url = $"{ApiConfig.BaseUrl}auth/login";
```

### Option 2 : Configuration via appsettings (pour Blazor/MAUI)

Dans votre projet client, ajoutez :

**appsettings.json** (Debug)
```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5276/api/"
  }
}
```

**appsettings.Production.json** (Release)
```json
{
  "ApiSettings": {
    "BaseUrl": "http://192.168.1.79:5276/api/"
  }
}
```

---

## 🚀 Processus de Déploiement

### 1. Déploiement automatique via GitHub

Lorsque vous poussez sur `main` ou `master` :

```bash
git add .
git commit -m "Votre message"
git push origin master
```

Le workflow GitHub va :
1. ✅ Compiler le projet en mode Release
2. ✅ Sauvegarder `appsettings.Production.json` du serveur
3. ✅ Copier les nouveaux fichiers
4. ✅ Restaurer `appsettings.Production.json`
5. ✅ Redémarrer le service

### 2. Vérification après déploiement

```bash
# Vérifier que le service fonctionne
sudo systemctl status serapi.service

# Vérifier les logs
sudo journalctl -u serapi.service -f

# Tester l'API
curl http://192.168.1.79:5276/api/health
```

---

## 🔐 Sécurité - Checklist

- [x] ✅ Les mots de passe ne sont PAS dans `appsettings.json` (commité dans Git)
- [x] ✅ `appsettings.Production.json` est dans `.gitignore`
- [x] ✅ `appsettings.Development.json` est dans `.gitignore`
- [x] ✅ Le workflow préserve le fichier de configuration du serveur
- [x] ✅ Les permissions du fichier sur le serveur sont restrictives (600)

---

## 🛠️ Développement Local

Pour développer localement :

1. **Première fois** : Copiez `appsettings.Production.json` vers `appsettings.Development.json`
2. Modifiez les valeurs selon votre environnement local
3. Ce fichier ne sera jamais commité grâce au `.gitignore`

```bash
# Si vous développez sur votre PC avec connexion à la DB distante
# appsettings.Development.json est déjà configuré avec vos identifiants
dotnet run
```

---

## 📝 Notes Importantes

1. **Ne JAMAIS commiter de secrets dans Git**
   - Vérifiez toujours avant de commit : `git diff`
   - Si vous avez déjà commité un secret, utilisez `git filter-branch` pour le supprimer de l'historique

2. **Changement de mot de passe**
   - Modifiez `appsettings.Production.json` sur le serveur
   - Redémarrez le service : `sudo systemctl restart serapi.service`

3. **Variables d'environnement (alternative)**
   Si vous préférez, vous pouvez aussi utiliser des variables d'environnement :
   ```bash
   export ConnectionStrings__DefaultConnection="Host=..."
   ```

---

## 🆘 Dépannage

### Le service ne démarre pas après déploiement

```bash
# Vérifier les logs
sudo journalctl -u serapi.service -n 50

# Vérifier que appsettings.Production.json existe
ls -la /var/www/serapi/appsettings.Production.json
```

### La connexion à la base de données échoue

```bash
# Tester la connexion PostgreSQL
psql -h 192.168.1.79 -p 5497 -U admin_api01 -d db_agenda

# Vérifier que le mot de passe est correct dans appsettings.Production.json
sudo cat /var/www/serapi/appsettings.Production.json
```
