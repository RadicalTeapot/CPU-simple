namespace CPU.components
{
    public class MmioRouter : IMmioDevice
    {
        public void Register(byte baseOffset, byte size, IMmioDevice device)
        {
            if (size == 0)
                throw new ArgumentException("Size must be greater than 0.", nameof(size));

            int end = baseOffset + size;
            if (end > 256)
                throw new ArgumentException($"Registration overflows byte range: base {baseOffset} + size {size} = {end}.", nameof(size));

            foreach (var mapping in _mappings)
            {
                int existingEnd = mapping.BaseOffset + mapping.Size;
                if (baseOffset < existingEnd && mapping.BaseOffset < end)
                    throw new ArgumentException($"Registration [{baseOffset}..{baseOffset + size - 1}] overlaps with existing [{mapping.BaseOffset}..{existingEnd - 1}].");
            }

            _mappings.Add(new DeviceMapping(baseOffset, size, device));
        }

        public byte ReadRegister(byte offset)
        {
            if (FindDevice(offset, out var relativeOffset, out var device))
                return device.ReadRegister(relativeOffset);
            return 0x00;
        }

        public void WriteRegister(byte offset, byte value)
        {
            if (FindDevice(offset, out var relativeOffset, out var device))
                device.WriteRegister(relativeOffset, value);
        }

        private bool FindDevice(byte offset, out byte relativeOffset, out IMmioDevice device)
        {
            foreach (var mapping in _mappings)
            {
                if (offset >= mapping.BaseOffset && offset < mapping.BaseOffset + mapping.Size)
                {
                    relativeOffset = (byte)(offset - mapping.BaseOffset);
                    device = mapping.Device;
                    return true;
                }
            }
            relativeOffset = 0;
            device = null!;
            return false;
        }

        private readonly record struct DeviceMapping(byte BaseOffset, byte Size, IMmioDevice Device);
        private readonly List<DeviceMapping> _mappings = [];
    }
}
