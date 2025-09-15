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
        public Sprite sourceImage;
#endif
        public string guid;
        public string key;
        public int frameDelay = 10;
        public bool isCustomDelay;
        public bool isDefault;
        public int[] customFrameDelay;

        public Node() { }

        public Node(Node copyNode)
        {
#if UNITY_EDITOR
            sourceImage = copyNode.sourceImage;
#endif
            key = copyNode.key;
            frameDelay = copyNode.frameDelay;
            guid = copyNode.guid;
        }
    }

    public bool IsLoaded => loadedSprites != null;

    [SerializeField] private AnimationState state;
    [SerializeField] private SpriteRenderer spriteRenderer = null;
    [SerializeField] private Image spriteImage = null;
    [Header("이미지 크기")]
    [SerializeField] private Vector2 size;

    [Header("이미지 리스트")]
    public List<Node> spriteSheets = new List<Node>();

    private Dictionary<FixedString64Bytes, Sprite[]> loadedSprites = null;

    private class Baker : Baker<SpriteSheetAnimationAuthoring>
    {
        public override void Bake(SpriteSheetAnimationAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // authoring MonoBehaviour 인스턴스를 관리형 컴포넌트로 추가합니다.
            AddComponentObject(entity, authoring);

            AddComponent(entity, new SpriteSheetAnimationComponent(authoring.state.ToString(), true));
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

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            spriteImage = GetComponent<Image>();
    }

    public void Initialize()
    {
        Awake();
    }

    public void Reset()
    {
        loadedSprites = null;
    }

    private void OnDestroy()
    {
        loadedSprites = null;
    }

    public void OnUpdateAnimation(string key, int frame, ref int index)
    {
        if (!IsLoaded)
            return;

        if (frame < GetFrameDelay(index))
            return;

        SetAnimationByIndex(key, NextAnimationIndex(key, ref index));
    }

    private int NextAnimationIndex(string key, ref int index)
    {
        index++;

        if (index >= GetSpriteSheetCount(key))
        {
            index = 0;
        }

        return index;
    }

    public void LoadAnimations(bool force = false)
    {
        if (IsLoaded && !force)
            return;

        Dictionary<FixedString64Bytes, Sprite[]> loadSprites = new Dictionary<FixedString64Bytes, Sprite[]>();

        for (int i = 0; i < spriteSheets.Count; ++i)
        {
            if (TryLoadSprite(i, out var key, out var sprites))
                loadSprites[key] = sprites;
        }

        loadedSprites = loadSprites;
    }

    private bool TryLoadSprite(int spriteIndex, out string key, out Sprite[] sprites)
    {
        key = null;
        sprites = null;

        if (spriteIndex < 0)
            return false;

        if (spriteIndex >= spriteSheets.Count)
            return false;

        key = spriteSheets[spriteIndex].key;
        string guid = spriteSheets[spriteIndex].guid;

        var spriteResourcesPath = ResourcesTypeRegistry.Get().GetResourcesPath<Sprite>();
        sprites = spriteResourcesPath.LoadAll<Sprite>(guid);

        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogError($"{guid} 에 Sprite 가 없습니다.");
            return false;
        }

        return true;
    }

    public void SetAnimationByIndex(FixedString64Bytes key, int animationIndex)
    {
        if (loadedSprites == null)
            return;

        if (key.IsEmpty)
            return;

        if (loadedSprites.TryGetValue(key, out var sprites))
        {
            if (sprites.Length <= animationIndex)
                return;

            SetSprite(sprites[animationIndex]);
        }
    }

    private void SetSprite(Sprite sprite)
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

    public void SetSpriteSheetImages(List<(string key, Sprite[] sprites)> spritePairs)
    {
        if (loadedSprites == null)
            loadedSprites = new Dictionary<FixedString64Bytes, Sprite[]>();

        for (int i = 0; i < spritePairs.Count; i++)
        {
            if (loadedSprites == null)
                loadedSprites = new Dictionary<FixedString64Bytes, Sprite[]>();

            if (spritePairs[i].sprites == null || spritePairs[i].sprites.Length == 0)
                continue;

            loadedSprites[spritePairs[i].key] = spritePairs[i].sprites;
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

    public int GetSpriteSheetCount(FixedString64Bytes key)
    {
        if (loadedSprites == null)
            return 0;

        if (loadedSprites.TryGetValue(key, out var sprites))
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

        if (!node.isCustomDelay)
            return node.frameDelay;

        if (node.customFrameDelay == null || node.customFrameDelay.Length <= index)
            return node.frameDelay;

        return node.customFrameDelay[index];
    }

    /// <summary>
    /// 애니메이션 Key 값이 없으면 기본 값을 반환함
    /// </summary>
    public bool TryGetSpriteSheetIndex(FixedString64Bytes key, out int spriteSheetIndex, out FixedString64Bytes defaultKey)
    {
        defaultKey = string.Empty;
        spriteSheetIndex = 0;

        for (int index = 0; index < spriteSheets.Count; index++)
        {
            if (spriteSheets[index].key == key)
            {
                spriteSheetIndex = index;
                return true;
            }

            if (spriteSheets[index].isDefault)
            {
                spriteSheetIndex = index;
                defaultKey = spriteSheets[index].key;
            }
        }

        return false;
    }

    public int GetLayer()
    {
        return spriteRenderer != null ? spriteRenderer.sortingOrder : 0;
    }
}