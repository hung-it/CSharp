namespace VinhKhanhAudioGuide.App;

public class PoiData
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? District { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? ImageUrl { get; set; }
    public string? MapLink { get; set; }
}
