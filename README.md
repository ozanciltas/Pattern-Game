# Pattern Game

A portrait mobile puzzle game built in Unity. A wall slides toward the player with one region of it highlighted as the **target pattern**. The key piece is that exact shape — drag it onto the target before the wall arrives. Every other placement is a loss, including landing on the wall's other cells.

## Screenshots

![ezgif-1-566da01204](https://github.com/user-attachments/assets/b2fa0684-9c05-48f5-9f63-e74744c47a22)

---

## How it plays

- The board is 4 columns by 5 rows. Cells are **empty**, **wall**, or **target**.
- A level is cleared when the piece sits exactly on the target — the board is never "filled in", this is a pattern match rather than a jigsaw.
- The match is evaluated once, at the moment the wall reaches the board.
- Levels are generated procedurally from a seed. Difficulty curves control pattern size, wall size, how scattered the wall is, and approach speed.
- The level number is the score; the best run is kept between sessions.

## Architecture

Seven runtime assemblies plus a test assembly. Every assembly definition lists its references explicitly and disables auto-referencing, so an illegal dependency is a compile error rather than a code review comment:

```
Core         → (none)                 StateMachine, DeterministicRandom, IPointerInput
Grid         → (none)                 GridMask, GridDefinition
Gameplay     → Core, Grid             LevelGenerator, Playfield, GameSession, flow states
Input        → Core, Input System     PointerInputService
Presentation → Core, Grid, Gameplay   MaskView, WallController, KeyPieceController, effects
UI           → Core, Gameplay, TMP    HudView
Bootstrap    → everything             GameBootstrapper, PlayerPrefsProgressStorage
```

**Game rules are plain C# classes; MonoBehaviours are thin views.** The flow states talk to the scene through `IWallPresenter`, `IPiecePresenter`, `IPieceInput`, `IEffectPresenter` and `IHudPresenter`, all declared in the Gameplay layer and implemented by the view components. The same inversion keeps the Input System out of Gameplay (`IPointerInput` lives in Core) and `PlayerPrefs` out of it too (`IProgressStorage`). The result is that the entire game loop runs in EditMode tests with fakes — no scene, no play mode.

Dependency injection is a hand-written composition root. `GameBootstrapper` is the only class that knows the whole object graph; there is no container and no third-party package.

## Design decisions

**The board is a 20-bit mask in a single `uint`.** `GridMask` hard-codes 4×5 because the representation depends on the cell count fitting in 32 bits. Matching becomes one integer comparison, and tests can sweep every one of the 2²⁰ possible boards instead of sampling.

**No physics, no colliders, no raycasts against the wall.** The result is `pieceMask == targetMask`, evaluated when the wall clamps to its arrival distance — a frame spike can never tunnel the wall past the check.

**No `Time.timeScale`, and exactly one `Update` in the project.** `GameBootstrapper.Update` ticks the state machine, which decides what else gets ticked. Pausing is simply not ticking.

**Solvability is guaranteed by construction.** The generator grows the target inside a randomly chosen box, picks a spawn far enough away, then scatters the remaining wall cells around it. The wall contains the target because it was built from it — nothing is generated and then validated in a retry loop.

**Null objects over null checks.** Optional presenters (match effects, HUD) fall back to do-nothing implementations, so the game runs with an unwired inspector field instead of throwing.

**Object pooling was deliberately left out.** The board's 20 cells are created once and toggled with an XOR diff between masks; particle bursts are three short-lived objects per cleared level. Pooling would add lifetime bugs to buy nothing measurable.

## Tests

172 EditMode tests covering the grid representation, level generation, the playfield rules, session progression, drag handling and the full game flow. Several are exhaustive rather than example-based — bit counting is checked against a naive implementation for every possible board, and level evaluation is swept across thousands of seeds and every legal placement.

The test assembly can see Core, Grid and Gameplay, and deliberately cannot see Presentation, Input or UI.

## Running it

- Unity **6000.3.9f1**, Universal Render Pipeline
- Open `Assets/_Project/Scenes/Game.unity` and press Play
- Tests: **Window ▸ General ▸ Test Runner ▸ EditMode**

## Technical details

- **Engine:** Unity 6.3 LTS (URP)
- **Language:** C#
- **Input:** Input System package
- **Target:** portrait mobile
