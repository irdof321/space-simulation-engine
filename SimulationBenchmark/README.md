# Space Simulation — Visualisation de la rotation propre des corps

Ce patch s'applique à la version **SimulationBenchmark modulaire** qui possède déjà :
- les deux systèmes stellaires et Comet C1 ;
- les compteurs de performance ;
- le découplage `PhysicsUpdatesPerSecond` / `AnimationFramesPerSecond` ;
- `AxialRotation` et `QuaternionDouble` dans la bibliothèque `PhysicsSimulation` (31 tests verts).

## Installation

1. Fais une copie de sauvegarde de ton projet `SimulationBenchmark`.
2. Copie les dossiers `Core/` et `UI/` du patch dans le projet, en **remplaçant seulement** les fichiers de même nom.
3. Les deux fichiers existants modifiés sont :
   - `Core/OrbitalScene.cs` : ajoute une propriété `BodyMarker.Spin` et l'initialise lors de la création de chaque corps ;
   - `UI/OrbitalLiveForm.cs` : ajoute la case **Show axial rotation** dans l'en-tête, pour ouvrir/fermer une fenêtre séparée.
4. Les quatre **nouveaux** fichiers sont :
   - `Core/BodyRotationCatalog.cs` : périodes, axes et phases de rotation de démonstration ;
   - `UI/RotationPreviewForm.cs` : fenêtre de sélection d'un corps et commandes de lecture ;
   - `UI/BodyRotationCanvas.cs` : rendu d'une sphère 3D orientée par les quaternions ;
   - `UI/ProceduralBodySurface.cs` : texture synthétique déterministe de chaque corps.
5. Relance `RunMode.LiveOrbits` sous Visual Studio.

**N'écrase pas** : `UI/OrbitalCanvas.cs`, `Configuration/`, `Program.cs`, `Live/`, `Tests/`, `PhysicsSimulation/` ni tes autres fichiers. Le patch ne modifie aucun intégrateur, aucune orbite, ni les six vues orbitales et leurs traces.

## Utilisation

1. Dans la fenêtre des orbites, coche **Show axial rotation** (en haut à droite).
2. Dans la fenêtre ouverte, choisis **Body** : Sun, Earth, Moon, Mars, Jupiter, Aster, Nysa, Nysa Moon, Vesper, Glacia ou Comet C1 selon la scène configurée.
3. **Preview** : 3, 8, 16 ou 32 secondes par tour **à l'écran**. Le temps de la simulation et les orbites ne sont pas accélérés par cette commande.
4. Coche **Follow simulation time** pour montrer l'orientation à `Simulation.Time`. Attention : avec 6 heures par pas physique et plusieurs pas par rafraîchissement, une planète comme la Terre peut sembler immobile ou tourner irrégulièrement (aliasing temporel normal).
5. Décoche la case du programme principal ou ferme la fenêtre de prévisualisation pour masquer le globe.

## Conventions et limites

- La rotation de la surface utilise vraiment `AxialRotation.GetOrientation(time)` puis le quaternion et sa conjugaison pour calculer le rendu.
- Les motifs sont générés de façon **déterministe** depuis le nom du corps, pour permettre de suivre les continents, bandes ou taches d'une image à l'autre. Ce n'est pas une simulation de géologie, d'atmosphère ni une véritable carte photographique.
- Périodes sidérales approximatives pour Sun, Earth, Moon, Mars et Jupiter ; axes visualisés de manière **illustrative** dans le repère du benchmark ; périodes/axes des corps fictifs générés de façon reproductible. Ces paramètres sont configurables dans `Core/BodyRotationCatalog.cs`.
- Le rendu graphique utilise une caméra virtuelle choisie pour voir la rotation ; il ne modifie jamais les positions 3D des corps du solveur.
- Cette version garde les métadonnées de rotation dans `BodyMarker.Spin` (métadonnée de scène), sans imposer un changement à l'API `CelestBody` du moteur physique. Lorsque les données de rotation seront officiellement partagées par la simulation et Unity, il sera pertinent de les déplacer dans un composant physique commun.
- L'affichage WinForms, la génération de texture et les timers sont sur le thread UI. La prévisualisation est petite et limitée à 20 images par seconde, mais elle ajoute un peu de charge graphique.

## Vérification conseillée

- La fenêtre d'orbites doit être visuellement inchangée, mis à part la case à cocher.
- Sélectionne Earth à 8 secondes par tour : les motifs doivent tourner tandis que les orbites continuent normalement.
- Sélectionne Moon puis Jupiter : les textures doivent être différentes et reproductibles au fil des ouvertures.
- Coche et décoche `Follow simulation time`, puis change le nombre de pas par update : l'effet d'aliasing est attendu, ce n'est pas un problème de quaternion.
- Relance tes 31 tests et compare les diagnostics d'énergie/momentum avant et après : aucun changement physique n'est attendu.

## Validation

Patch construit par regroupement des versions sources partagées : refactor, deux systèmes avec comète, compteurs de performance, découplage d'affichage et tests axiaux. Revues structurelles effectuées. Le conteneur ici ne contient pas de compilateur .NET/WinForms, donc **la compilation et l'exécution sur Windows restent à faire**.
