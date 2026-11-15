# Laurel

*Des succès mérités, jamais offerts.*

**Autres langues :** [Español](../LEEME.md) · [English](README.en.md) · [Português](README.pt-BR.md) · [Deutsch](README.de.md) · [Русский](README.ru.md) · [简体中文](README.zh-CN.md) · [日本語](README.ja.md)

Prépare vos jeux pour que **Hydra Launcher enregistre les succès quand vous les obtenez en jouant**.
L'app ne débloque jamais de succès d'elle-même.

## Utilisation
1. Ouvrez `App\Laurel.exe` et **choisissez votre langue**. Il y en a 8 : espagnol, anglais, portugais, français, allemand, russe, chinois et japonais. Si vous cochez « Ne plus demander », vous pourrez la changer avec le bouton **Langue**.
2. La première fois, l'app cherche seule le dossier de téléchargement de Hydra (elle lit seulement ses données, sans les modifier). Si vos jeux sont ailleurs, cliquez sur **Ajouter…**.
3. Saisissez votre **clé API Steam**. Elle est gratuite et s'obtient avec le lien « Obtenir une clé ». Sans elle, l'app peut analyser vos jeux mais pas les préparer. **La clé n'est jamais enregistrée :** elle reste uniquement en mémoire et vous devrez la saisir à chaque ouverture de l'app.
4. Cliquez sur **Préparer les jeux réparables**.
5. Laissez-la ouverte ou réduisez-la (elle reste près de l'horloge). Quand vous installez un jeu, elle attend la fin de l'installation et le prépare automatiquement.

**Astuce :** les cartes colorées au-dessus de la liste comptent les jeux de chaque état ; cliquez sur l'une d'elles pour n'afficher que ces jeux. La recherche filtre par nom, émulateur ou AppID.

## Signification des états
| État | Signification |
|---|---|
| OK | Hydra trouve déjà le fichier de succès de ce jeu |
| PRÊT | Configuré ; le fichier apparaîtra avec votre premier succès |
| STEAM | `steam_api.dll` est l'original de Valve (signature vérifiée) : Hydra lit les succès depuis votre compte Steam |
| RÉPARABLE | Utilise Goldberg mais la liste des succès manque ; l'app peut la créer |
| SANS APPID | Clic droit sur le jeu → « Définir l'AppID » |
| NON PRIS EN CHARGE | La DLL ne vient pas de Valve et ne contient aucune signature d'émulateur connu : son émulateur n'enregistre pas les succès là où Hydra les cherche |
| SANS STEAM | N'utilise pas Steam. L'app indique la plateforme utilisée : GOG (avec ou sans succès), Epic, Ubisoft, Microsoft Store ou aucune |

## Protection de vos fichiers
- Elle **n'écrase jamais** un fichier existant : elle crée seulement ceux qui manquent.
- Elle **ne supprime jamais** rien d'elle-même.
- Elle note chaque fichier créé dans `cambios.json`.
- **Annuler les modifications** envoie à la Corbeille uniquement les fichiers créés par l'app que personne n'a modifiés depuis. Si vous ou le jeu les avez modifiés, ils sont conservés.
- Le **mode simulation** montre ce qu'elle ferait sans rien écrire.

## Identificateur de DLL
Quand un jeu a une `steam_api.dll` mais aucun fichier de configuration connu, l'app examine la DLL **en lecture seule** :
1. Elle vérifie la signature numérique avec Windows, sans se connecter à Internet. Si la signature est celle de Valve et qu'elle est valide, le jeu est une copie Steam.
2. Elle cherche dans le fichier des signatures d'émulateurs connus : Goldberg, CODEX, RUNE, EMPRESS, TENOKE, OnlineFix, CreamAPI, SmartSteamEmu, 3DM, ALI213, SKIDROW, RLD! et Razor1911.

Le résultat apparaît dans « Voir les détails » (clic droit sur le jeu).

## Ponts de succès
Certains jeux enregistrent bien les succès, mais à un endroit que Hydra ne lit pas. Avec **Ponts de succès** activé, l'app copie ces succès **déjà obtenus** au format compris par Hydra (`%AppData%\GSE Saves\<AppID>\achievements.json`). Elle n'invente jamais de succès.
- **GOG Galaxy → Hydra :** pour les jeux GOG avec succès. Définissez leur AppID Steam (clic droit → « Définir l'AppID »). L'app lit une *copie temporaire* de la base de données de GOG Galaxy, jamais l'originale. Si vous avez saisi votre clé API, seuls les succès dont le nom correspond à Steam sont copiés.
- **RLE → Hydra :** Hydra connaît le dossier de RLE (`%AppData%\RLE`) mais ne l'analyse pas ; le pont s'en charge.
- Si le fichier de destination existe déjà et n'a pas été créé par l'app (par exemple parce que le jeu utilise Goldberg), il **n'est pas modifié**.
- Tout ce qu'un pont crée est noté dans `cambios.json` et peut être annulé.

## Guide pour les jeux NON PRIS EN CHARGE
L'app **n'installe pas d'émulateurs**. Pour ces jeux, clic droit → **« Guide pour suivre ses succès… »** ouvre un guide pas à pas :
1. **Sauvegarde** de la DLL actuelle (en `.hydralogros-original`, sans rien écraser).
2. Lien vers le dépôt officiel de gbe_fork.
3. Ouvre le dossier de la DLL pour que **vous** la remplaciez.
4. **Vérifier :** si gbe_fork est détecté, le jeu est préparé automatiquement.

Proposé uniquement pour les copies dont la DLL n'est plus l'originale de Valve.

## Confidentialité et utilisation de l'API Steam
- **Ce qui est enregistré sur le disque :** uniquement les bibliothèques, les options (y compris la langue), les AppID que vous définissez et la liste des fichiers créés (pour pouvoir annuler). Jamais la clé API. Si un ancien `config.json` la contenait, l'app la supprime au démarrage.
- **Le journal ne contient aucune donnée personnelle :** dans `registro.txt` et à l'écran, votre dossier utilisateur apparaît comme `%USERPROFILE%`, votre nom d'utilisateur comme `<utilisateur>`, et les adresses e-mail et la clé sont masquées.
- **L'app interroge Steam avec prudence** pour que l'usage de votre clé reste normal :
  - une seule requête à la fois, avec au moins 3 secondes d'intervalle ;
  - au maximum 150 requêtes par session ;
  - chaque jeu n'est interrogé qu'une fois par session ;
  - si Steam refuse la clé, elle n'est plus utilisée tant que vous n'en saisissez pas une autre ;
  - si Steam demande de ralentir, les requêtes sont suspendues 15 minutes.
- La liste des succès est téléchargée depuis Steam dans la langue choisie.

## Sécurité et analyse
**Ce qui est analysé :** uniquement les sous-dossiers de vos bibliothèques, en ne cherchant que les fichiers qui identifient émulateurs et plateformes. L'app n'entre pas dans les dossiers audio, vidéo, ressources ou installeurs, et masque les dossiers sans fichiers de jeu. Elle vérifie plusieurs jeux à la fois, avec une limite de taille et de temps par jeu.

**Verrous :**
- **Bibliothèques :** impossible d'utiliser comme bibliothèque la racine d'un disque, Windows, Program Files, ProgramData, Utilisateurs, votre profil, AppData ou le dossier temporaire. Ni les liens symboliques. S'ils figurent dans `config.json`, ils sont ignorés.
- **Liens :** ne suit jamais les liens symboliques ni les *junctions*, et ne lit pas les fichiers OneDrive disponibles uniquement en ligne (ne les télécharge pas).
- **Écriture :** ne peut créer que `steam_settings\achievements.json`, `steam_settings\steam_appid.txt`, la sauvegarde `.hydralogros-original` et `GSE Saves\<AppID>\achievements.json`. Tout autre chemin est bloqué.
- **Annuler :** même si quelqu'un modifie `cambios.json`, seule cette liste de fichiers est concernée.
- **Réseau :** ne se connecte qu'à `api.steampowered.com` en HTTPS et ne suit pas les redirections ; votre clé ne peut donc pas finir sur un autre serveur. N'ouvre que deux sites de confiance.
- **Rien n'est exécuté :** ne lance jamais de programme depuis les dossiers de jeux. Les dossiers s'ouvrent avec l'Explorateur et les DLL de Windows ne sont chargées que depuis `System32`.
- **Données d'entrée :** les AppID ne peuvent être que des nombres, les fichiers texte de plus de 1 Mo sont ignorés et les recherches de texte ont un temps maximal.
- **Journal :** `registro.txt` est renouvelé à 2 Mo.
- **Avertissement** si vous lancez l'app en administrateur (inutile).

## Fichiers du dossier
- `App\Laurel.exe` : l'application. Nécessite le .NET 10 Desktop Runtime.
- `App\config.json`, `cambios.json`, `estado.json` et `registro.txt` : créés par l'app à l'usage.
- `leeme\` : ce document dans d'autres langues.
- `src\` : code source. Pour recompiler : `dotnet publish src -c Release -o App`.
- `tools\IconGen\` : génère l'icône (`src\app.ico`). Pour la régénérer : `dotnet run --project tools/IconGen -- src/app.ico`.
- `Portable\Laurel.exe` : version portable. Elle embarque .NET (rien à installer) et garde ses données dans son propre dossier. Pour la recompiler : `dotnet publish src -c Release -p:Portable=true -o Portable`.
- Le bouton **Lisez-moi** de l'app affiche ce document dans votre langue. Il utilise une copie intégrée au `.exe` : modifier ces `.md` ne change pas ce qu'affiche l'app (il faut recompiler).

## Licence
Laurel est un logiciel libre : vous pouvez l'utiliser, l'étudier, le modifier et le partager selon les termes de la [Licence publique générale GNU v3.0](../LICENSE) (GPL-3.0). Si vous distribuez une version modifiée, elle doit aussi être libre et accompagnée de son code source.

*** CET OUTIL EST LIBRE D'UTILISATION ***
