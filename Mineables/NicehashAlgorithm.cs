namespace CryptonightProfitSwitcher.Mineables
{
    public class NicehashAlgorithm : Mineable
    {
        public int ApiId { get; set; }
        public override string Id => $"NiceHashAlgo{ApiId}";
    }
}
