using System;
using UnityEngine;
using Unity.Entities;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Collections;

public class SpriteSheetAnimationAuthoring : MonoBehaviour
{
    /// <summary>
    /// UnityEngine 기능이 있는 데이터라서
    /// 여기에 포함함
    /// </summary>
    [Serializable]
    public class Node
    {
#if UNITY_EDITOR
        public Sprite SourceImage;
#endif
        public string Guid;
        public AnimationState State;
        public int FrameDelay = 10;
        public bool IsCustomDelay;
        public bool IsDefault;
        public int[] CustomFrameDelay;
        public int SpriteCount;

        public Node() { }

        public Node(Node copyNode)
        {
#if UNITY_EDITOR
            SourceImage = copyNode.SourceImage;
#endif
            State = copyNode.State;
            FrameDelay = copyNode.FrameDelay;
            Guid = copyNode.Guid;
        }
    }

    public bool IsLoaded => loadedSprites != null;

    [SerializeField] private AnimationState defaultState;
    [SerializeField] private SpriteRenderer spriteRenderer = null;
    [SerializeField] private Image spriteImage = null;
    [Header("이미지 크기")]
    [SerializeField] private Vector2 size;

    [Header("이미지 리스트")]
    public List<Node> spriteSheets = new List<Node>();

    private Dictionary<AnimationState, Sprite[]> loadedSprites = null;

    private class Baker : Baker<SpriteSheetAnimationAuthoring>
    {
        public override void Bake(SpriteSheetAnimationAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            authoring.Initialize();

            // authoring MonoBehaviour 인스턴스를 관리형 컴포넌트로 추가합니다.
            AddComponentObject(entity, authoring);

            AddComponent(entity, new SpriteSheetAnimationComponent(authoring.defaultState, true));
        }

    }
    
    private void OnValidate()
    {
        if(spriteRenderer != null || TryGetComponent(out spriteRenderer))
        {
            size = spriteRenderer.size;
        }
        else if (spriteImage != null || TryGetComponent(out spriteImage))
        {
            RectTransform rectTransform = transform as RectTransform;

            if(rectTransform != null)
                size = rectTransform.sizeDelta;
        }
    }

    public void Initialize()
    {
        if (!spriteRenderer && !spriteImage)
        {
            if (!spriteRenderer)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            if (!spriteRenderer && !spriteImage)
            {
                spriteImage = GetComponentInChildren<Image>();
            }
        }
    }

    public void Reset()
    {
        loadedSprites = null;
    }

    private void OnDestroy()
    {
        loadedSprites = null;
    }

    public void OnUpdateAnimation(AnimationState state, int frame, ref int index)
    {
        if (!IsLoaded)
            return;

        if (frame < GetFrameDelay(index))
            return;

        SetAnimationByIndex(state, NextAnimationIndex(state, ref index));
    }

    private int NextAnimationIndex(AnimationState state, ref int index)
    {
        index++;

        if (index >= GetSpriteSheetCount(state))
        {
            index = 0;
        }

        return index;
    }

    public void LoadAnimations(bool force = false)
    {
        if (IsLoaded && !force)
            return;

        Dictionary<AnimationState, Sprite[]> loadSprites = new Dictionary<AnimationState, Sprite[]>();

        for (int i = 0; i < spriteSheets.Count; ++i)
        {
            if (TryLoadSprite(i, out var state, out var sprites))
                loadSprites[state] = sprites;
        }

        loadedSprites = loadSprites;
    }

    private bool TryLoadSprite(int spriteIndex, out AnimationState state, out Sprite[] sprites)
    {
        state = AnimationState.Idle;
        sprites = null;

        if (spriteIndex < 0)
            return false;

        if (spriteIndex >= spriteSheets.Count)
            return false;

        state = spriteSheets[spriteIndex].State;
        string guid = spriteSheets[spriteIndex].Guid;

        var spriteResourcesPath = ResourcesTypeRegistry.Get().GetResourcesPath<Sprite>();
        sprites = spriteResourcesPath.LoadAll<Sprite>(guid);

        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogError($"{guid} 에 Sprite 가 없습니다.");
            return false;
        }

        return true;
    }

    public void SetFlip(bool isFlip)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = isFlip;
        }
        else if (spriteImage != null)
        {
            var rotation = spriteImage.transform.rotation.eulerAngles;
            rotation.x = isFlip ? 180f : 0;
            spriteImage.transform.rotation = Quaternion.Euler(rotation);
        }
    }

    public void SetAnimationByIndex(AnimationState state, int animationIndex)
    {
        if (loadedSprites == null)
            return;

        if (loadedSprites.TryGetValue(state, out var sprites))
        {
            if (sprites.Length <= animationIndex)
                return;

            SetSprite(sprites[animationIndex]);
        }
    }

    public void SetSprite(Sprite sprite)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;

            if(size.x > 0 && size.y > 0)
                spriteRenderer.size = size;
        }
        else if (spriteImage != null)
        {
            spriteImage.sprite = sprite;

            if (size.x > 0 && size.y > 0)
                (transform as RectTransform).sizeDelta = size;
        }
    }

    public void SetSize(Vector2 size)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.size = size;
        }
        else if (spriteImage != null)
        {
            spriteImage.rectTransform.sizeDelta = size;
        }
    }

    public void SetFlip(bool flipX, bool flipY)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = flipX;
            spriteRenderer.flipY = flipY;
        }
        else if (spriteImage != null)
        {
            spriteImage.rectTransform.localScale = new Vector3(flipX ? -1 : 1, flipY ? -1 : 1, 0);
        }
    }

    public void SetLayer(int layer)
    {
        if (spriteRenderer != null)
            spriteRenderer.sortingOrder = layer;
    }

    public void SetAlpha(float alpha)
    {
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }

        if(spriteImage != null)
        {
            Color color = spriteImage.color;
            color.a = alpha;
            spriteImage.color = color;
        }
    }

    public void SetSpriteSheetImages(List<(AnimationState state, Sprite[] sprites)> spritePairs)
    {
        if (loadedSprites == null)
            loadedSprites = new Dictionary<AnimationState, Sprite[]>();

        for (int i = 0; i < spritePairs.Count; i++)
        {
            if (loadedSprites == null)
                loadedSprites = new Dictionary<AnimationState, Sprite[]>();

            if (spritePairs[i].sprites == null || spritePairs[i].sprites.Length == 0)
                continue;

            loadedSprites[spritePairs[i].state] = spritePairs[i].sprites;
        }
    }

    public Vector2 GetSize()
    {
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            return spriteRenderer.size;
        }
        else if (spriteImage != null && spriteImage.sprite != null)
        {
            return spriteImage.rectTransform.sizeDelta;
        }

        return size;
    }

    public (bool flipX, bool flipY) GetFlip()
    {
        if (spriteRenderer != null)
        {
            return (spriteRenderer.flipX, spriteRenderer.flipY);
        }
        else if (spriteImage != null)
        {
            Vector3 scale = spriteImage.rectTransform.localScale;

            return (scale.x < 0, scale.y < 0);
        }

        return (false, false);
    }

    public int GetSpriteSheetCount(AnimationState state)
    {
        if (loadedSprites == null)
            return 0;

        if (loadedSprites.TryGetValue(state, out var sprites))
        {
            return sprites.Length;
        }

        return 0;
    }

    public int GetFrameDelay(int index)
    {
        if (spriteSheets.Count <= index || index < 0)
            return 0;

        Node node = spriteSheets[index];

        if (!node.IsCustomDelay)
            return node.FrameDelay;

        if (node.CustomFrameDelay == null || node.CustomFrameDelay.Length <= index)
            return node.FrameDelay;

        return node.CustomFrameDelay[index];
    }

    public Node GetDefaultSpriteNode(out int index)
    {
        index = spriteSheets.FindIndex(ss => ss.IsDefault);

        return spriteSheets[index];
    }

    /// <summary>
    /// 애니메이션 Key 값이 없으면 기본 값을 반환함
    /// </summary>
    public bool TryGetSpriteNode(AnimationState state, out Node findNode, out int findIndex)
    {
        findNode = null;
        findIndex = -1;

        for (int index = 0; index < spriteSheets.Count; index++)
        {
            if (spriteSheets[index].State == state)
            {
                findNode = spriteSheets[index];
                findIndex = index;
                return true;
            }

            if (spriteSheets[index].IsDefault)
            {
                findNode = spriteSheets[index];
                findIndex = index;
            }
        }

        return false;
    }

    public int GetLayer()
    {
        return spriteRenderer != null ? spriteRenderer.sortingOrder : 0;
    }
}