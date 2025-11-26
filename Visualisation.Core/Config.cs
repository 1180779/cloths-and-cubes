namespace Visualisation.Core;

public static class Config
{
    public static class Pbr
    {
        public static string CacheDirectory = Path.Join("cache", "pbr");
        public static string BrfdLuftCacheFile = "brdf_lut.bin";
    }
}