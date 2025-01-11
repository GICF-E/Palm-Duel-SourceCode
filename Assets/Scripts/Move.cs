using System;

[Serializable]
public struct Ability
{
    // 能量类型
    public string key;
    // 能量消耗量
    public int value;
}

[Serializable]
public struct Move
{
    // 招数名称
    public string name;
    // 招数消耗能力点类型和数量
    public Ability[] abilityCost;
    // 招数增加的能力点
    public Ability[] abilityIncrease;
    // 招数造成的伤害
    public float damage;
    // 防御的伤害
    public float defense;
    // 加血
    public float heal;
    // 招数的克制
    public Ability[] restrain;
}

[Serializable]
public class MovesList
{
    public Move[] moves;
}
