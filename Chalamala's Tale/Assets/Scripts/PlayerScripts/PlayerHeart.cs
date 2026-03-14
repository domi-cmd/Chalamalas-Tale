using UnityEngine;
using UnityEngine.UI;

public class PlayerHeart : MonoBehaviour
{
    // Fields for the three different sprites a heart can have
    public Sprite fullHeart, halfHeart, emptyHeart;
    Image heartImage;

    private void Awake()
    {
        heartImage = GetComponent<Image>();
    }

    // Sets the image of the heart based on its status (full, half or empty)
    public void SetHeartImage(HeartStatus status)
    {
        switch(status)
        {
            case HeartStatus.Empty:
                heartImage.sprite = emptyHeart;
                break;

            case HeartStatus.Half:
                heartImage.sprite = halfHeart;
                break;

            case HeartStatus.Full:
                heartImage.sprite = fullHeart;
                break;
        }
    }
}

public enum HeartStatus 
{
    Empty = 0,
    Half = 1,
    Full = 2
}