namespace Baubit.Mediation
{
    public interface IResponse
    {
        public long Id { get; }

        public long ForRequest { get; }
    }
}
