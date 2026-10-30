# Tests de la preview sur personnage

Compiler l'application avec `Build-CharacterPreview.ps1`, puis :

```powershell
dotnet build .\Tests\CharacterPreview\PreviewTests.csproj -c Release
& '.\Tests\CharacterPreview\bin\Release\net48\PreviewTests.exe' 'D:\chemin\costumes.grf' '.\work-tests'
```

Ce premier mode examine 20 ACT d'accessoires présents dans le GRF fourni, construit les deux personnages et vérifie toutes les frames, les indices de sprites et l'absence de modification de l'ACT sélectionné.

Pour la régression WPF objet au sol / version portée, utiliser un GRF contenant `anya_wig_xmas.act` et ses versions portées masculine/féminine :

```powershell
& '.\Tests\CharacterPreview\bin\Release\net48\PreviewTests.exe' 'D:\chemin\costumes.grf' '.\work-tests-visuels' visual
```

Le test affiche brièvement la preview, vérifie le rendu des actions et frames, le changement de sexe, le masquage, la restauration du fichier d'origine et la sauvegarde du réglage. Trois PNG sont générés dans le dossier de sortie. Le GRF source reste inchangé. Aucun costume de test n'est distribué avec les tests.
