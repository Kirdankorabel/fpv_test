namespace Input
{
    public interface IPilotInputProvider
    {
        PilotCommand Read();
        void Reset();
    }
}
