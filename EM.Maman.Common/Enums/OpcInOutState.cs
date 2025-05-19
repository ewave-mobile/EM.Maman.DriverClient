namespace EM.Maman.Common.Enums
{
    public enum OpcInOutState : short // Assuming the register takes a short
    {
        Idle = 0,
        InFromLeft = 1,
        InFromRight = 2,
        OutFromLeft = 3,
        OutFromRight = 4
    }
}
