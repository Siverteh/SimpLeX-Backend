namespace SimpLeX_Backend.Models
{
    public class BlockChangeMessage
    {
        public string Action { get; set; } // "blockChange"
        public string Data { get; set; } // JSON string of the block change details
    }
}