using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace QR.Api {
  [DataContract]
  public record PrintCmd {
    [JsonConstructor]
    public PrintCmd(string fileName, string bucket, int times = 2) =>
        (FileName, Bucket, Times) = (fileName, bucket, times);

    [DataMember]
    [JsonPropertyName("file_name")]
    public string FileName { get; init; }

    [DataMember] [JsonPropertyName("bucket")] public string Bucket { get; init; }

    [DataMember] [JsonPropertyName("times")] public int Times { get; init; }
  }
}