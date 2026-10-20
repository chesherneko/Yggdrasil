using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlueMarble
{
    [RequireComponent(typeof(BoardBuilder))]
    public class BoardBuilderUI : MonoBehaviour
    {
        [SerializeField] private Toggle propertyToggle;
        [SerializeField] private Toggle specialToggle;
        [SerializeField] private Toggle cornerToggle;
        [SerializeField] private Button createButton;
        [SerializeField] private Button removeButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private TMP_Dropdown spaceDropdown;
        
        private TileType currentType = TileType.Property;
        private int currentSpace = 0;
        
        private BoardBuilder builder;
        
        private DataManager Data => DataManager.Instance;

        private void Awake()
        {
            builder = GetComponent<BoardBuilder>();
        }

        private async void Start()
        {
            await Data.EnsureDataLoaded();

            InitSpaceDropdown();
            
            OnToggleChanged(propertyToggle, TileType.Property);
            OnToggleChanged(specialToggle, TileType.Special);
            OnToggleChanged(cornerToggle, TileType.Corner);
            createButton.onClick.AddListener(OnCreateButtonClicked);
            removeButton.onClick.AddListener(OnRemoveButtonClicked);
            saveButton.onClick.AddListener(OnSaveButtonClicked);
            loadButton.onClick.AddListener(OnLoadButtonClicked);
            clearButton.onClick.AddListener(OnClearButtonClicked);
            spaceDropdown.onValueChanged.AddListener(OnSpaceDropdownValueChanged);
        }

        private void InitSpaceDropdown()
        {
            List<string> spaceNames = Data.BoardSpaces.GetDefs()
                .Select(def => def.Name)
                .ToList();
            
            spaceDropdown.AddOptions(spaceNames);
            spaceDropdown.value = 0;
        }

        private void OnToggleChanged(Toggle toggle, TileType type)
        {
            toggle.onValueChanged.AddListener(value =>
            {
                if (value == false) return;
                currentType = type;
            });
        }

        private void OnCreateButtonClicked() => builder.CreateTile(currentType, currentSpace);

        private void OnRemoveButtonClicked() => builder.RemoveLastTile();
        
        private void OnSaveButtonClicked() => builder.SaveBoard();

        private void OnLoadButtonClicked() => builder.LoadBoard();
        
        private void OnClearButtonClicked() => builder.ClearBoard();
        
        private void OnSpaceDropdownValueChanged(int value) => currentSpace = value;
    }
}