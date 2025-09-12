using System;
using UnityEngine;
using Unity.Entities;
using UnityEngine.UI;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

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

    public bool IsLoaded { get; private set; }
    public string CurrentKey { get; private set; }

    [SerializeField]
    private SpriteRenderer spriteRenderer = null;
    [SerializeField]
    private Image spriteImage = null;
    [Header("이미지 크기")]
    [SerializeField]
    public Vector2 size;

    [Header("이미지 리스트")]
    [SerializeField]
    public List<Node> spriteSheets = new List<Node>();

    private Dictionary<string, Sprite[]> loadedSprites = null;
    private int currentSpriteSheetIndex = 0;
    private int currentAnimationIndex = 0;
    private int currentAnimationCount = 0;
    private int passingFrame = 0;
    private bool isLoop = true;

    private class Baker : Baker<SpriteSheetAnimationAuthoring>
    {
        public override void Bake(SpriteSheetAnimationAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // authoring MonoBehaviour 인스턴스를 관리형 컴포넌트로 추가합니다.
            AddComponentObject(entity, authoring);

            AddComponent(entity, new SpriteSheetAnimationComponent());

            AddBuffer<SpriteSheetNodeBuffer>(entity);
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

#if UNITY_EDITOR
    /// <summary>
    /// 에디터용
    /// </summary>
    public void InitializeByEditor()
    {
        Awake();
    }
#endif

    private void Update()
    {
        OnUpdateAnimation();
    }

    public void Reset()
    {
        IsLoaded = false;
        loadedSprites = null;
    }

    private void OnDestroy()
    {
        IsLoaded = false;

        loadedSprites = null;
    }

    public void OnUpdateAnimation()
    {
        if (!IsLoaded)
            return;

        if (!CheckAnimationFrame())
            return;

        NextAnimation();
    }

    private bool CheckAnimationFrame()
    {
        if (passingFrame < GetFrameDelay())
        {
            passingFrame++;
            return false;
        }
        else
        {
            passingFrame = 0;
            return true;
        }
    }

    public async UniTask LoadAnimationsAsync(bool force = false)
    {
        if (IsLoaded && !force)
            return;

        UniTask[] tasks = new UniTask[spriteSheets.Count];

        for (int i = 0; i < spriteSheets.Count; ++i)
        {
            tasks[i] = LoadSpriteAsync(i);
        }

        await UniTask.WhenAll(tasks);

        IsLoaded = true;
    }

    private async UniTask LoadSpriteAsync(int spriteIndex)
    {
        if (spriteIndex < 0)
            return;

        if (spriteIndex >= spriteSheets.Count)
            return;

        string key = spriteSheets[spriteIndex].key;
        string guid = spriteSheets[spriteIndex].guid;

        if (loadedSprites != null && loadedSprites.ContainsKey(key))
            return;

        var spriteResourcesPath = ResourcesTypeRegistry.Get().GetResourcesPath<Sprite>();
        Sprite[] currentSprites = spriteResourcesPath.LoadAll<Sprite>(guid);

        if (loadedSprites == null)
            loadedSprites = new Dictionary<string, Sprite[]>();

        if (currentSprites == null || currentSprites.Length == 0)
        {
            Debug.LogError($"{guid} 에 Sprite 가 없습니다.");
            return;
        }

        loadedSprites[key] = currentSprites;
    }

    public void SetAnimation(string key, bool isLoop = true)
    {
        this.isLoop = isLoop;

        SetSpriteSheet(key);
    }

    private void SetSpriteSheet(string key)
    {
        CurrentKey = GetKey(key, out currentSpriteSheetIndex);
        currentAnimationIndex = 0;
        currentAnimationCount = GetSpriteSheetCount(CurrentKey);

        SetAnimationByIndex(currentAnimationIndex);
    }

    private void SetAnimationByIndex(int animationIndex)
    {
        if (loadedSprites == null)
            return;

        if (string.IsNullOrEmpty(CurrentKey))
            return;

        if (loadedSprites.TryGetValue(CurrentKey, out var sprites))
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
            loadedSprites = new Dictionary<string, Sprite[]>();

        for (int i = 0; i < spritePairs.Count; i++)
        {
            if (loadedSprites == null)
                loadedSprites = new Dictionary<string, Sprite[]>();

            if (spritePairs[i].sprites == null || spritePairs[i].sprites.Length == 0)
                continue;

            loadedSprites[spritePairs[i].key] = spritePairs[i].sprites;
        }
    }

    private void NextAnimation()
    {
        SetAnimationByIndex(NextAnimationIndex());
    }

    private int NextAnimationIndex()
    {
        currentAnimationIndex++;

        if (currentAnimationIndex >= currentAnimationCount)
        {
            if (isLoop)
                currentAnimationIndex = 0;
            else
                currentAnimationIndex = currentAnimationCount - 1;
        }

        return currentAnimationIndex;
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

    private int GetSpriteSheetCount(string key)
    {
        if (loadedSprites == null)
            return 0;

        if (loadedSprites.TryGetValue(key, out var sprites))
        {
            return sprites.Length;
        }

        return 0;
    }

    private int GetFrameDelay()
    {
        if (spriteSheets.Count <= currentSpriteSheetIndex || currentSpriteSheetIndex < 0)
            return 0;

        Node node = spriteSheets[currentSpriteSheetIndex];

        if (!node.isCustomDelay)
            return node.frameDelay;

        if (node.customFrameDelay == null || node.customFrameDelay.Length <= currentAnimationIndex)
            return node.frameDelay;

        return node.customFrameDelay[currentAnimationIndex];
    }

    /// <summary>
    /// 애니메이션 Key 값이 없으면 기본 값을 반환함
    /// </summary>
    private string GetKey(string key, out int spriteSheetIndex)
    {
        string defaultkey = string.Empty;
        spriteSheetIndex = 0;

        for (int index = 0; index < spriteSheets.Count; index++)
        {
            if (spriteSheets[index].key == key)
            {
                spriteSheetIndex = index;
                return key;
            }

            if (spriteSheets[index].isDefault)
            {
                spriteSheetIndex = index;
                defaultkey = spriteSheets[index].key;
            }
        }

        return defaultkey;
    }

    public int GetLayer()
    {
        return spriteRenderer != null ? spriteRenderer.sortingOrder : 0;
    }
}