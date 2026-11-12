# Stockify

Stockify is a work-in-progress storage and supply mod for RimWorld 1.6 and 1.7. It uses real item stacks and vanilla hauling jobs; it does not teleport or synthesize materials.

## Current functionality

- Mod-list metadata and version-specific folders for RimWorld 1.6 and 1.7.
- Persistent map storage networks that discover vanilla storage buildings inside a designated area.
- Connected warehouse shelves can be included/excluded from the network UI and the choice is saved.
- Overlapping warehouse areas do not double-count a storage building; the earlier network owns overlapping buildings unless that shelf is explicitly excluded.
- A Stockify Shelf building with saved warehouse and workbench assignments.
- Per-material minimum, target, and maximum stock controls, plus shelf naming and transfer priorities.
- Searchable warehouse material exclusions and configurable warehouse transfer priority.
- A warehouse management window and a designation tool for creating or resizing storage areas.
- Vanilla hauling jobs for:
  - INPUT: moving eligible loose items into enabled warehouse shelves only when vanilla storage selection chooses that warehouse as the best destination.
  - GET: search materials, select a warehouse and compatible Stockify Shelf, and request a quantity; the dialog estimates unreserved stock, subtracts outstanding requests, caps fulfillment, and reports any shortfall.
  - PULL: replenishing an assigned shelf from selected warehouses based on active bills (including bill ingredient restrictions) or configured stock levels.
  - OUTPUT: returning excess shelf stock to its selected warehouses while retaining active bill requirements.
- Vanilla shelf filters, storage acceptance rules, item forbidden state, and pawn reservations are respected by these hauling jobs.

## Limitations

- This is not a finished implementation of the full development specification.
- GET can be cancelled; its distinct Stockify hauling jobs are interrupted through vanilla job cleanup, releasing their reservations. Progress is shown on the shelf inspect panel rather than in a transfer queue.
- Warehouse overview quantities are keyed by ThingDef and represent physical stored stacks. GET separately estimates unreserved quantities from vanilla reservations and in-flight hauling jobs; a stack reserved by another vanilla job is not counted as requestable.
- Per-material filters on individual Stockify shelves still use their vanilla storage tab; recovering interrupted requests with detailed per-job status is incomplete.
- A headless RimWorld 1.7.5005 startup was attempted with Stockify enabled and reached `UIRoot` without Stockify-specific log entries or the earlier language/initialization warnings. The headless run then produced input-related `KeyBindingDef` exceptions in `ScreenshotTaker`; it does not verify interactive use, hauling, cancellation, or save/load.
- GET/PULL/INPUT/OUTPUT and save/load behavior have not been tested interactively. Do not treat successful compilation or headless startup as gameplay validation.
- The 1.6 project currently compiles against the assemblies available in this environment; a separate RimWorld 1.6 installation was not available to verify its APIs or runtime behavior.
- Validation performed: both version projects built successfully with 0 warnings and 0 errors; all 5 XML files parsed and had no duplicate `defName` values. No automated test project exists in this repository.
- No release ZIP is included.

## Install

Copy this folder into RimWorld's `Mods` directory, then enable Stockify in the in-game Mods menu. `LoadFolders.xml` selects the matching version folder and shared definitions.

## Build

Requires the .NET SDK and RimWorld assemblies. Set the `RimWorldDir` MSBuild property if the game installation is not in the default Steam location.

```powershell
dotnet build Source\Compatibility\V1_7\Stockify.V1_7.csproj
dotnet build Source\Compatibility\V1_6\Stockify.V1_6.csproj
```

The assemblies are written to `1.7\Assemblies\Stockify.dll` and `1.6\Assemblies\Stockify.dll`.
