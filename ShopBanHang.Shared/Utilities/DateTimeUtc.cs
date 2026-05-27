using System;

namespace ShopBanHang.Shared.Utilities
{
    public static class DateTimeUtc
    {
        public static DateTime Normalize(DateTime value)
        {
            if (value == default)
                return DateTime.UtcNow;

            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }

        public static DateTime? Normalize(DateTime? value) =>
            value.HasValue ? Normalize(value.Value) : null;
    }
}
