public static class LayerUtil
{
    /// <summary>
    /// 可交互物体
    /// </summary>
    public const int Interactable = 30;
    public const int Interactable_Mask = 1 << Interactable;
    
    /// <summary>
    /// 碰撞体
    /// </summary>
    public const int Obstacle = 31;
    public const int ObstacleMask = 1 << Obstacle;
}