using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ithappy
{
    public class UiManager : MonoBehaviour
    {
        public Action<int> OnSwitchCar;
        public Action<CarElementName, int> OnSwitchCarElement;

        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _prevButton;
        [SerializeField] private CarUiElement _carUiElementPrefab;
        [SerializeField] private RectTransform _carUiElementParent;

        private List<CarUiElement> _currentCarUiElements = new List<CarUiElement>();

        public void Initialize(List<CarElementSettings> carElements)
        {
            InitNewCar(carElements);
            _nextButton.onClick.AddListener(NextButtonClick);
            _prevButton.onClick.AddListener(PrevButtonClick);
        }

        public void Dispose()
        {
            DisposeCarUiElements();
            
            _nextButton.onClick.RemoveAllListeners();
            _prevButton.onClick.RemoveAllListeners();
        }

        private void NextButtonClick()
        {
            OnSwitchCar?.Invoke(1);
            EventSystem.current.SetSelectedGameObject(null);
        }

        private void PrevButtonClick()
        {
            OnSwitchCar?.Invoke(-1);
            EventSystem.current.SetSelectedGameObject(null);
        }

        public void SwitchCar(List<CarElementSettings> carElements)
        {
            InitNewCar(carElements);
        }

        private void DisposeCarUiElements()
        {
            foreach (CarUiElement oldElement in _currentCarUiElements)
            {
                oldElement.Dispose();
                Destroy(oldElement.gameObject);
            }

            _currentCarUiElements.Clear();
        }

        private void InitNewCar(List<CarElementSettings> carElements)
        {
            DisposeCarUiElements();

            CarUiElement carUiElement;
            foreach (var item in carElements)
            {
                if (item.Elements.Count <= 1)
                {
                    continue;
                }

                carUiElement = Instantiate(_carUiElementPrefab, _carUiElementParent);
                carUiElement.Initialize(item);
                carUiElement.OnValueChanges += ValueChanges;
                _currentCarUiElements.Add(carUiElement);
            }
        }

        private void ValueChanges(CarUiElement uiElement, int index)
        {
            OnSwitchCarElement?.Invoke(uiElement.ElementName, index);
        }
    }
}
