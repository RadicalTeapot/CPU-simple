
## Gamepad register

The gamepad is a single read-only register at its device's relative offset `0x00`. Each bit represents one button; a `1` means the button is currently held. The exact button-to-bit mapping is TBD and will be defined when the gamepad is implemented.