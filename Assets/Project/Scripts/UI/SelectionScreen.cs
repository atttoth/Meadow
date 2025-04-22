using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectionScreen : MonoBehaviour
{
    private Image _blackOverlay;
    private TextMeshProUGUI _selectText;
    private List<ScreenDisplayItem> _deckItems;
    private SelectionScreenLayout _selectionScreenLayout;

    public List<Button> Init()
    {
        _blackOverlay = transform.GetChild(0).GetComponent<Image>();
        _selectText = transform.GetChild(5).GetComponent<TextMeshProUGUI>();
        _deckItems = new();
        for (int i = 0; i < (int)DeckType.NUM_OF_DECKS; i++)
        {
            ScreenDisplayItem item = transform.GetChild(1 + i).GetComponent<ScreenDisplayItem>();
            item.Init();
            item.type = (DeckType)i;
            item.mainImage.sprite = GetBackImageOfDeck((DeckType)item.type);
            _deckItems.Add(item);
        }
        _selectionScreenLayout = new SelectionScreenLayout(GetComponent<RectTransform>(), _deckItems.First().mainImage.GetComponent<RectTransform>());
        return _deckItems.Select(item => item.button).ToList();
    }

    private Sprite GetBackImageOfDeck(DeckType deckType)
    {
        switch (deckType)
        {
            case DeckType.West: return GameResourceManager.Instance.West.GetSprite("back");
            case DeckType.South: return GameResourceManager.Instance.South.GetSprite("back");
            case DeckType.East: return GameResourceManager.Instance.East.GetSprite("back");
            default: return GameResourceManager.Instance.North.GetSprite("back");
        }
    }

    public void ToggleDeckSelectionScreenHandler(GameTask task, DeckType deckType, bool value)
    {
        switch(task.State)
        {
            case 0:
                if (value)
                {
                    _blackOverlay.enabled = true;
                    List<Vector2> positions = _selectionScreenLayout.GetCenteredPositions(_deckItems.Count - 1);
                    DeckType[] currentTypes = new DeckType[3] { DeckType.West, deckType, DeckType.East };
                    _deckItems
                        .Where(item => Array.Exists(currentTypes, type => type == (DeckType)item.type))
                        .ToList()
                        .ForEach(item =>
                        {
                            item.button.enabled = true;
                            Vector2 pos = positions.First();
                            item.GetComponent<RectTransform>().position = new(pos.x, pos.y + _selectionScreenLayout.GetPosYOffset());
                            positions.RemoveAt(0);
                            item.gameObject.SetActive(true);
                        });
                }
                else
                {
                    _deckItems.Where(item => (DeckType)item.type != deckType).ToList().ForEach(item => item.gameObject.SetActive(false));
                    _deckItems.Find(item => (DeckType)item.type == deckType).button.enabled = false;
                    _selectText.enabled = false;
                }
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void ShowCardSelectionHandler(GameTask task, List<Card> cards, bool isPlayerSelection)
    {
        switch (task.State)
        {
            case 0:
                _blackOverlay.enabled = isPlayerSelection;
                List<Vector2> positions = _selectionScreenLayout.GetCenteredPositions(cards.Count);
                cards.ForEach(card =>
                {
                    card.SetParentTransform(transform);
                    card.transform.SetParent(transform);
                    card.GetComponent<RectTransform>().position = positions.First();
                    positions.RemoveAt(0);
                    card.gameObject.SetActive(true);
                });
                task.StartDelayMs(500);
                break;
            case 1:
                int duration = (int)(GameSettings.Instance.GetDuration(Duration.cardRotationSpeedOnBoard) * 1000);
                cards.ForEach(card => card.FlipDeckCardTween(true));
                task.StartDelayMs(duration);
                break;
            case 2:
                cards.ForEach(card =>
                {
                    card.CardIconItemsView.Toggle(true);
                    card.ToggleSelection(isPlayerSelection);
                    card.ToggleRayCast(isPlayerSelection);
                });
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void HideCardSelectionHandler(GameTask task, List<Card> cards, bool isPlayerSelection)
    {
        switch (task.State)
        {
            case 0:
                task.StartDelayMs(isPlayerSelection ? 0 : 500);
                break;
            case 1:
                cards.ForEach(card =>
                {
                    card.CardIconItemsView.Toggle(false);
                    card.FlipDeckCardTween(false);
                    card.ToggleRayCast(false);
                });
                task.StartDelayMs((int)(GameSettings.Instance.GetDuration(Duration.cardRotationSpeedOnBoard) * 1000));
                break;
            case 2:
                cards.ForEach(card => card.gameObject.SetActive(false));
                if(cards.Count > 1) // ignore at initial ground card pick
                {
                    _deckItems.Find(item => (DeckType)item.type == cards.First().Data.deckType).gameObject.SetActive(false);
                }
                task.StartDelayMs(500);
                break;
            case 3:
                _blackOverlay.enabled = false;
                task.StartDelayMs(500);
                break;
            default:
                task.Complete();
                break;
        }
    }
}
