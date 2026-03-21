## Controller (Gamepad)

The controller is a read-only MMIO device that exposes gamepad input to the CPU as a single status register. External code (e.g. the emulator frontend) sets button state from a separate thread; the CPU reads the register to poll input.

### Architecture

```
Frontend thread           Controller thread boundary           CPU thread
─────────────────         ──────────────────────────         ───────────────
ButtonsState.SetUp()     ──→  lock(_lock): _directions[Up] = true (latched)
ButtonsState.SetButton() ──→  lock(_lock): write custom btn
                                                           ←── ReadRegister(0x00)
                                                               ReadAndReset() [lock]
                                                               → returns snapshot
                                                               → clears state
```

`ButtonsState` is the public-facing input surface. `ControllerRegisters` is the MMIO-facing output surface. They share a `ButtonsState` instance, with all cross-thread access protected by a single `Lock`.

### MMIO Register Map

| Offset | Name   | Access | Description          |
|--------|--------|--------|----------------------|
| `0x00` | STATUS | R      | Button state bitmask |

All other offsets return `0x00` on read. Writes to any offset are silently ignored.

### STATUS Register (`0x00`)

Reading STATUS returns the bitmask of all buttons pressed since the last read, then **atomically clears all state**. Bits that were never set return `0`.

| Bit | Button   |
|-----|----------|
| 0   | Up       |
| 1   | Down     |
| 2   | Left     |
| 3   | Right    |
| 4   | Button 0 |
| 5   | Button 1 |
| 6   | Button 2 |
| 7   | Button 3 |

A `1` means the button was pressed (latched) since the last STATUS read. The register is cleared on read, so polling code should cache the value locally if multiple bits need to be tested.

### Configuration

`ControllerConfiguration(int ButtonCount)` sets the number of custom buttons (0–4). Configuring more than 4 buttons throws `TooManyButtonsException` during `ButtonsState` construction (before any STATUS reads occur), since 8-bit builds only have bits 4–7 available; a 16-bit extension is noted in the TODO comment in `ControllerRegisters`.

### Thread Safety

`ButtonsState` is thread-safe. All reads and writes are protected by a `Lock`. The STATUS read is atomic: `ReadAndReset()` snapshots all state and clears it in a single lock acquisition, so no button press can be lost or double-counted between a read and a reset.

### Usage Example

```csharp
// Setup (once)
var config = new ControllerConfiguration(ButtonCount: 2);
var controller = new Controller(config);
// Register with the MMIO bus
mmioRouter.Register(baseAddress, 1, controller.Registers);

// Input thread (e.g. Raylib key polling)
if (IsKeyDown(KeyboardKey.Up)) controller.ButtonState.SetUp();
if (IsKeyDown(KeyboardKey.A))  controller.ButtonState.SetButton(0);

// CPU assembly — poll the controller
//   lda r0, [CONTROLLER_STATUS]   ; read and clear
//   and r0, #0x01                 ; test Up bit
//   jnz [handle_up]
```

### Emulator Integration

The emulator exposes the controller at the application level via `--buttons N` (or `"buttons": N` in `emulator.json`). This requires `--vram` to also be set; specifying `--buttons` without `--vram` is rejected at startup.

In windowed mode (`--vram` present), `Display.PollInput` polls Raylib keyboard state once per frame (before `TickFrame`) and pushes updates to the controller's `ButtonsState`. In headless mode there is no window and therefore no input source — `--buttons` without `--vram` is rejected for this reason.

All Raylib key reads are contained in `Display.PollInput`; no other file calls Raylib input APIs. The default key mapping is:

| Key | Action |
|-----|--------|
| Arrow Up | Direction: Up |
| Arrow Down | Direction: Down |
| Arrow Left | Direction: Left |
| Arrow Right | Direction: Right |
| Z | Button 0 |
| X | Button 1 |
| A | Button 2 |
| S | Button 3 |

Keys are polled with `IsKeyDown` (not `IsKeyPressed`), so holding a key keeps the latch set each frame until the CPU reads and clears the STATUS register. `CpuHandler.ButtonState` (`ButtonsState?`) is the bridge between `CpuHandler` and the emulator loop — it returns null when no controller is configured, which suppresses the poll call entirely.

For programmatic use (e.g. in tests), construct a `PeripheralSet` directly:

```csharp
var peripherals = new PeripheralSet(ppuConfig, chrData, new ControllerConfiguration(ButtonCount: 2));
var context = new EmulatorApplication.EmulatorContext(
    logger, input, output, cpuConfig,
    Peripherals: peripherals,
    DisplayScale: 4
);
```
