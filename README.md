# Stockify - RimWorld 1.6/1.7 Storage Network Mod

## Overview
Stockify is an intelligent storage network and quality-of-life mod that connects existing vanilla shelves through player-designated storage areas. Players can select an area using a dedicated selection tool, and vanilla shelves within that area become registered members of a storage network.

## Core Features

### Storage Networks
- Designate storage areas using a tool similar to the vanilla Home Area tool
- Automatically detect and register eligible vanilla shelves
- Support multiple independent networks on the same map
- Each network has its own name, settings, and configuration

### GET System
- Request specific materials from connected storage networks
- Search by material name and specify quantities
- Partial requests when insufficient materials are available
- Automatic source shelf selection with reservation system

### PULL System (Signature Feature)
- Compatible vanilla workbenches can retrieve crafting ingredients from connected networks
- Detect ingredient requirements from production bills
- Reserve physical item stacks and transfer them to workbench-accessible locations
- Controlled material-transfer system that minimizes colonist hauling trips

### INPUT System
- Control which materials can enter each network
- Automatically accept compatible hauled materials
- Respect shelf filters and storage priorities
- Check available storage capacity

### OUTPUT System
- Control which materials can leave each network
- Support workbench output destinations
- Priority-based destination selection
- Prevent unintended removal of restricted items

### Smart Storage Management
- Three selection modes: Nearest First, Priority First, Balanced
- Intelligent shelf selection based on distance, priority, and capacity
- Respect vanilla storage restrictions at all times

### User Interface
- Clean, vanilla-style network windows
- Material inventory with available/reserved/total quantities
- Four independent toggles: GET, PULL, INPUT, OUTPUT
- Network gizmos: Open, Rename, Enable/Disable, Expand Area, Clear Designation

### Save/Load Support
- All network configurations survive saving and loading
- Uses RimWorld's Scribe serialization system
- Rebuilds inventory caches after loading
- Safe recovery from interrupted transfers

## Installation
1. Download the mod ZIP archive
2. Extract to RimWorld's Mods folder: `RimWorld/Mods/Stockify/`
3. Launch RimWorld and enable Stockify from the mod menu
4. Choose RimWorld 1.6 or 1.7 version appropriate for your game

## Version Support
- **RimWorld 1.7**: net10.0 target framework, Harmony 3
- **RimWorld 1.6**: net472 target framework, Harmony 2

## Building from Source
- Requires Visual Studio 2022 or .NET 8 SDK
- Solution file: Stockify.sln
- Build targets in Source/Shared (shared code) and Source/Compatibility/V1_6/V1_7 (version-specific)

## Known Limitations
- Mod only works with vanilla shelves; modded storage buildings require compatible APIs
- Physical-item transfer system; virtual ingredient access not implemented
- Multiple networks on same map but no cross-network material sharing
- Performance scales with network size; large colonies may see minor overhead

## Development Status
- Phase 1: Foundation (Mod initialization, designation tool, network creation, save/load) - Complete
- Phase 2: Storage Management (Inventory calculations, material search, network settings) - Planned
- Phase 3: GET System (Material requests, reservations, transfers) - Planned
- Phase 4: PULL System (Workbench integration, bill detection) - Planned
- Phase 5: Input and Output (Material flow management) - Planned
- Phase 6: UI and Optimization (Final interface, performance) - Planned
- Phase 7: Testing and Release - Planned