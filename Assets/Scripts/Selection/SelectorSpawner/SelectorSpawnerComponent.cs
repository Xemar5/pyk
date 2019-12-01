using Unity.Entities;

public struct SelectorSpawnerComponent : IComponentData
{
    public SelectorMode mode;
    public Entity selectorPrefab;
    public float range;
}
