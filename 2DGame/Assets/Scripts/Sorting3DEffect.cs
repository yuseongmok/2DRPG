using UnityEngine;

public class Sorting3DEffect : MonoBehaviour
{
    public string sortingLayerName = "Effect"; // 우리가 만든 2D 레이어 이름
    public int sortingOrder = 10;              // 앞뒤 순서 번호

    void Start()
    {
        // 3D 에셋이 가지고 있는 MeshRenderer를 찾습니다.
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        
        if (meshRenderer != null)
        {
            // 스크립트로 3D 메쉬에 2D 정렬 레이어를 강제로 심어버립니다.
            meshRenderer.sortingLayerName = sortingLayerName;
            meshRenderer.sortingOrder = sortingOrder;
        }
    }
}