using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "ScriptableObject/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("무기 기본 정보")]
    public string weaponName;       
    public int attackDamage = 10;   
    public float attackRate = 2.5f;

    public Sprite weaponSprite; //외형
    public float attackRange = 0.5f; //범위

    [TextArea(3, 5)] 
    public string weaponDescription;

    [Header("애니메이션 설정")]
    public string attackAnimationStateName = "Player_Attack"; 

    [Header("이펙트 설정")]
    public GameObject attackEffectPrefab; 
    public GameObject hitEffectPrefab;    

    [Header("무기 고유 스킬 설정")]
    public string skillName;             // 스킬 이름 (예: "강격", "파이어 볼")
    public float skillCooldown = 5f;     // 스킬 쿨타임 (초 단위)
    public int skillDamage = 25;
    public float skillRange = 1.2f; //범위
    public string skillAnimationName;    // 스킬 전용 애니메이션 상태 이름
    public GameObject skillEffectPrefab; // 스킬 사용 시 뿜어져 나올 이펙트 프리팹
}