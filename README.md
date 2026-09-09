# Blast puzzle demo

A Unity demo with a deterministic C# board for connected-group detection,
gravity, seeded refill, scoring and line-clearing power-ups.

![Blast Puzzle Systems Demo](Docs/blast-puzzle-demo.png)

## Design

`BlastBoard` owns the gameplay rules without depending on scenes or frame timing.

- Breadth-first search finds orthogonally connected groups without recursion.
- Collapse and refill produce the same board for the same seed and player input.
- Large groups create rockets. Each result returns the accepted state, removals,
  score and created power-up to the presenter.

The runtime bootstrap creates the interface from Unity primitives and uGUI.
The scene contains no serialized game state.

See [the architecture note](Docs/ARCHITECTURE.md) for boundaries and tradeoffs.

## Run

1. Install Unity **2022.3.61f1** with macOS build support.
2. Open the repository folder and let Unity restore the declared packages.
3. Choose **Demo > Create clean scene**, then enter Play Mode.
4. Select any group of two or more connected colors. Groups of five or more
   create a rocket.

## Test and build

Six EditMode tests cover invalid moves, cluster boundaries, gravity, seeded
behavior and both power-up paths.

```sh
UNITY="/Applications/Unity/Hub/Editor/2022.3.61f1/Unity.app/Contents/MacOS/Unity"
"$UNITY" -batchmode -projectPath "$PWD" -runTests -testPlatform editmode \
  -testResults "$PWD/Builds/editmode-results.xml" -quit

"$UNITY" -batchmode -projectPath "$PWD" -buildTarget StandaloneOSX \
  -executeMethod OguzhanOzdemir.BlastPuzzle.Editor.DemoProjectBuilder.BuildMac \
  -buildOutput "$PWD/Builds/macOS/BlastPuzzleDemo.app" -quit
```

## License and provenance

MIT licensed. This independent demo by Oguzhan Ozdemir develops an earlier
personal Unity exercise using original code and generated UI primitives.
It contains no studio art, audio or branding.
