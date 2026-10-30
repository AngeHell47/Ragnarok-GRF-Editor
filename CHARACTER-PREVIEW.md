# Preview des hats sur un personnage

Dans **Animation preview**, **Show character** affiche ou masque un personnage de reference. Le reglage est memorise. **Auto / Male / Female** permet de choisir le personnage ; Auto suit le dossier masculin ou feminin du sprite.

En partant d'un sprite d'objet au sol (`data/sprite/아이템`, affiche `¾ÆÀÌÅÛ` avec le codage occidental), la preview cherche sa version portee dans `data/sprite/악세사리` (`¾Ç¼¼»ç¸®`). Les noms avec prefixe de sexe et sans prefixe sont reconnus. La recherche reste dans le GRF ouvert. Masquer le personnage retablit l'animation du fichier selectionne. L'en-tete indique `(worn preview)` quand la version portee est affichee, et son chemin est disponible dans l'infobulle.

Le personnage suit les directions et les frames de l'animation. Le corps et la tete s'attachent aux ancrages du hat. Les ACT/SPR et le GRF ne sont pas modifies par cette preview.

Compiler avec `Build-CharacterPreview.ps1` sous Windows (Visual Studio / Build Tools, .NET Framework 4.8 et outils WPF). Cette commande compile le projet de l'application directement, la solution publique referencant un projet Encryption non distribue. La DLL fournie dans Libraries est utilisee. Une seconde passe incorpore les dependances compressees, prises en charge par le chargeur existant. Le resultat se trouve dans `GRFEditor/bin/CharacterPreview` ; l'EXE et son fichier `.exe.config` suffisent pour utiliser l'application.

Sources : [Tokeiburu/GRFEditor](https://github.com/Tokeiburu/GRFEditor), revision `66032976a3caef5a1bfb5c9a46dfe0048df46efb`. Les huit ressources de personnage `ref_body_*` et `ref_head_*` proviennent de [Tokeiburu/ActEditor](https://github.com/Tokeiburu/ActEditor/tree/c12198b6aaaedcaa1a3850a8d06e0618c2d4fd0c/ActEditor/Resources), avec leurs noms d'origine. Les ajustements des HintPath du projet utilisent les DLL distribuees dans Libraries. `SkipResourceCompression` permet d'omettre le batch reserve aux icones, deja presentes dans le depot.

Validation : compilation Release, 20 hats reels, deux sexes, 55 432 frames de references, ACT originaux identiques apres la preview ; regression WPF sur `anya_wig_xmas.act`, resolution objet/porte, rendu de toutes ses actions et frames, affichage/masquage, changement de sexe et sauvegarde du reglage.
