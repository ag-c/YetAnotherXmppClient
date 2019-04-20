
using System.ComponentModel.DataAnnotations;

namespace YetAnotherXmppClient.Persistence.Model
{
    public class Avatar
    {
        [Key]
        public string Sha1Hash { get; set; }
        public byte[] ImageBytes { get; set; }
    }
}
