using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;

public class DeckSelectionScreen : MonoBehaviour
{
    private Image _blackOverlay;
    private TextMeshProUGUI _selectText;
    private List<ScreenItem> _deckItems;

    public List<Button> Init()
    {
        _blackOverlay = transform.GetChild(0).GetComponent<Image>();
        _selectText = transform.GetChild(5).GetComponent<TextMeshProUGUI>();
        _deckItems = new();
        for (int i = 0; i < (int)DeckType.NUM_OF_DECKS; i++)
        {
            ScreenItem item = transform.GetChild(1 + i).GetComponent<ScreenItem>();
            item.deckType = (DeckType)i;
            item.transform.GetChild(0).GetComponent<Image>().sprite = GetBackImageOfDeck(item.deckType);
            _deckItems.Add(item);
        }
        return _deckItems.Select(item => item.GetComponent<Button>()).ToList();
    }

    private Sprite GetBackImageOfDeck(DeckType deckType)
    {
        switch(deckType)
        {
            case DeckType.West: return GameAssets.Instance.West.GetSprite("back");
            case DeckType.South: return GameAssets.Instance.South.GetSprite("back");
            case DeckType.East: return GameAssets.Instance.East.GetSprite("back");
            default: return GameAssets.Instance.North.GetSprite("back");
        }
    }

    public void ToggleDeckSelectionScreen(bool value, DeckType deckType)
    {
        if(value)
        {
            _blackOverlay.enabled = true;
            List<float> positions = new() { -300f, 0f, 300f };
            DeckType[] currentTypes = new DeckType[3] { DeckType.West, DeckType.East, deckType };
            _deckItems
                .Where(item => Array.Exists(currentTypes, type => type == item.deckType))
                .ToList()
                .ForEach(item =>
                {
                    item.GetComponent<Button>().enabled = true;
                    item.GetComponent<RectTransform>().anchoredPosition = new(positions.First(), 300f);
                    positions.RemoveAt(0);
                    item.gameObject.SetActive(true);
                });
        }
        else
        {
            _deckItems.Where(item => item.deckType != deckType).ToList().ForEach(item => item.gameObject.SetActive(false));
            _deckItems.Find(item => item.deckType == deckType).GetComponent<Button>().enabled = false;
            _selectText.enabled = false;
        }
    }

    public void ShowCardSelectionHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                List<float> positions = new() { -300f, 0f, 300f };
                task.Data.topCards.ForEach(card =>
                {
                    card.SetParentTransform(transform);
                    card.transform.SetParent(transform);
                    card.GetComponent<RectTransform>().anchoredPosition = new(positions.First(), 0f);
                    positions.RemoveAt(0);
                    card.gameObject.SetActive(true);
                });
                task.StartDelayMs(500);
                break;
            case 1:
                int duration = (int)(ReferenceManager.Instance.gameLogicManager.GameSettings.cardRotationSpeedOnBoard * 1000);
                task.Data.topCards.ForEach(card => card.FlipDeckCard());
                task.StartDelayMs(duration);
                break;
            case 2:
                task.Data.topCards.ForEach(card => card.ToggleSelection(true));
                task.StartDelayMs(0);
                break;
            default:
            task.Complete();
            break;
        }
    }

    public void HideCardSelectionHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                int duration = (int)(ReferenceManager.Instance.gameLogicManager.GameSettings.cardRotationSpeedOnBoard * 1000);
                task.Data.topCards.ForEach(card =>
                {
                    card.FlipDeckCard(true);
                });
                task.StartDelayMs(duration);
                break;
            case 1:
                task.Data.topCards.ForEach(card => card.gameObject.SetActive(false));
                _deckItems.Find(item => item.deckType == task.Data.topCards.First().Data.deckType).gameObject.SetActive(false);
                task.StartDelayMs(500);
                break;
            case 2:
                _blackOverlay.enabled = false;
                task.StartDelayMs(500);
                break;
            default:
                task.Complete();
                break;
        }
    }
}
