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
    public Sprite skillIcon;
    public int skillDamage = 25;
    public float skillRange = 1.2f; //범위
    public string skillAnimationName;    // 스킬 전용 애니메이션 상태 이름
    public GameObject skillEffectPrefab; // 스킬 사용 시 뿜어져 나올 이펙트 프리팹
    public GameObject projectilePrefab; //원거리용

    [Header("스킬 진동 설정")]
    public float shakeDuration = 0.15f; // 진동이 지속될 시간 (초)
    public float shakeMagnitude = 0.2f; // 진동 세기 (기본 강도 0.1 ~ 0.3 추천)
}