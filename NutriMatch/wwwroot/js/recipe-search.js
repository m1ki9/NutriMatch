
let selectedIngredients = [];
let searchTimeout;
let currentFocus = -1;
let selectedIngredient = null;
const ingredientSearch = document.getElementById('ingredientSearch');
const ingredientDropdown = document.getElementById('ingredientDropdown');
const ingredientsList = document.getElementById('ingredientsList');
const hiddenInput = document.getElementById('selectedIngredients');
const addButton = document.getElementById('addIngredientButton');
const qtyInput = document.getElementById('ingredientQuantity');
const unitSelect = document.getElementById('ingredientUnit');
document.addEventListener('DOMContentLoaded', function() {
    initializeSearchFunctionality();
});
function initializeSearchFunctionality() {
    if (addButton) {
        addButton.addEventListener('click', function() {
            if (selectedIngredient && qtyInput && unitSelect) {
                const qty = parseFloat(qtyInput.value);
                const unit = unitSelect.value;
                if (qty > 0 && unit && selectedIngredient.name) {
                    addIngredient(selectedIngredient, qty, unit);
                } else {
                    alert('Please enter a valid quantity and select a unit.');
                }
            } else {
                alert('Please search and select an ingredient first.');
            }
        });
    }
    if (ingredientSearch) {
        ingredientSearch.addEventListener('input', function() {
            const query = this.value.trim();
            currentFocus = -1;
            selectedIngredient = null;
            if (query === '') {
                hideDropdown(ingredientDropdown);
                return;
            }
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => {
                searchIngredients(query);
            }, 300);
        });
        ingredientSearch.addEventListener('keydown', function(e) {
            handleKeyNavigation(e, ingredientDropdown);
        });
        ingredientSearch.addEventListener('focus', function() {
            if (this.value.trim() !== '') {
                searchIngredients(this.value.trim());
            }
        });
    }
    document.addEventListener('click', function(e) {
        if (!e.target.closest('.search-container')) {
            hideDropdown(ingredientDropdown);
        }
    });
}
async function searchIngredients(query) {
    if (!ingredientDropdown) return;
    try {
        ingredientDropdown.innerHTML = '<div class="loading">Loading...</div>';
        showDropdown(ingredientDropdown);
        const response = await fetch(`/Recipes/getSuggestions?query=${encodeURIComponent(query)}`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            }
        });
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        const suggestions = await response.json();
        displaySuggestions(suggestions, query);
    } catch (error) {
        console.error('Error fetching suggestions:', error);
        ingredientDropdown.innerHTML = '<div class="no-results">Error loading suggestions. Please try again.</div>';
        showDropdown(ingredientDropdown);
    }
}
function displaySuggestions(suggestions, query) {
    if (!ingredientDropdown) return;
    ingredientDropdown.innerHTML = '';
    if (!suggestions || suggestions.length === 0) {
        ingredientDropdown.innerHTML = '<div class="no-results">No results found</div>';
        showDropdown(ingredientDropdown);
        return;
    }
    suggestions.forEach((suggestion, index) => {
        const item = document.createElement('div');
        item.className = 'dropdown-item';
        item.setAttribute('data-index', index);
        const name = typeof suggestion === 'string' ? suggestion : suggestion.name;
        const id = typeof suggestion === 'object' ? suggestion.id : null;
        const regex = new RegExp(`(${escapeRegExp(query)})`, 'gi');
        const highlightedText = name.replace(regex, '<strong>$1</strong>');
        item.innerHTML = highlightedText;
        item.addEventListener('click', function() {
            selectedIngredient = {
                id: id,
                name: name
            };
            ingredientSearch.value = name;
            hideDropdown(ingredientDropdown);
        });
        ingredientDropdown.appendChild(item);
    });
    showDropdown(ingredientDropdown);
}
function handleKeyNavigation(e, dropdownElement) {
    if (!dropdownElement) return;
    const items = dropdownElement.querySelectorAll('.dropdown-item:not(.no-results):not(.loading)');
    switch(e.key) {
        case 'ArrowDown':
            e.preventDefault();
            currentFocus++;
            if (currentFocus >= items.length) currentFocus = 0;
            setActive(items);
            break;
        case 'ArrowUp':
            e.preventDefault();
            currentFocus--;
            if (currentFocus < 0) currentFocus = items.length - 1;
            setActive(items);
            break;
        case 'Enter':
            e.preventDefault();
            if (currentFocus > -1 && items[currentFocus]) {
                items[currentFocus].click();
            }
            break;
        case 'Escape':
            hideDropdown(dropdownElement);
            e.target.blur();
            break;
    }
}
function setActive(items) {
    items.forEach(item => item.classList.remove('highlighted'));
    if (currentFocus >= 0 && currentFocus < items.length) {
        items[currentFocus].classList.add('highlighted');
        items[currentFocus].scrollIntoView({ block: 'nearest' });
    }
}
function addIngredient(ingredient, quantity, unit) {
    if (!ingredient || !ingredient.name) {
        console.error('Invalid ingredient data');
        return;
    }
    const existingIngredient = selectedIngredients.find(item => item.id === ingredient.id);
    if (existingIngredient) {
        alert('This ingredient is already added to the recipe.');
        return;
    }
    const newIngredient = {
        id: ingredient.id,
        name: ingredient.name,
        quantity: quantity,
        unit: unit
    };
    selectedIngredients.push(newIngredient);
    updateIngredientsDisplay();
    updateIngredientsInput();
    if (ingredientSearch) ingredientSearch.value = '';
    if (qtyInput) qtyInput.value = '';
    if (unitSelect) unitSelect.value = '';
    selectedIngredient = null;
    hideDropdown(ingredientDropdown);
    currentFocus = -1;
}
function removeIngredient(ingredientName) {
    selectedIngredients = selectedIngredients.filter(item => item.name !== ingredientName);
    updateIngredientsDisplay();
    updateIngredientsInput();
}
function updateIngredientsDisplay() {
    if (!ingredientsList) return;
    if (selectedIngredients.length === 0) {
        ingredientsList.innerHTML = '<small class="text-muted">Selected ingredients will appear here</small>';
    } else {
        ingredientsList.innerHTML = selectedIngredients.map(ingredient => 
            `<span class="ingredient-tag" style="display: inline-block; background-color: #e3f2fd; padding: 5px 10px; margin: 2px; border-radius: 15px; font-size: 14px;">
                ${escapeHtml(ingredient.quantity)} ${escapeHtml(ingredient.unit)} of ${escapeHtml(ingredient.name)}
                <span class="remove" onclick="removeIngredient('${escapeHtml(ingredient.name)}')" 
                      style="cursor: pointer; margin-left: 8px; color: #dc3545; font-weight: bold;" 
                      title="Remove ingredient">&times;</span>
            </span>`
        ).join('');
    }
}
function updateIngredientsInput() {
    if (hiddenInput) {
        hiddenInput.value = JSON.stringify(selectedIngredients);
    }
}
function showDropdown(dropdownElement) {
    if (dropdownElement) {
        dropdownElement.style.display = 'block';
    }
}
function hideDropdown(dropdownElement) {
    if (dropdownElement) {
        dropdownElement.style.display = 'none';
    }
    currentFocus = -1;
}
function escapeHtml(text) {
    if (text === null || text === undefined) return '';
    const div = document.createElement('div');
    div.textContent = text.toString();
    return div.innerHTML;
}
function escapeRegExp(string) {
    return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
window.RecipeSearch = {
    addIngredient: addIngredient,
    removeIngredient: removeIngredient,
    getSelectedIngredients: () => [...selectedIngredients],
    clearIngredients: () => {
        selectedIngredients = [];
        updateIngredientsDisplay();
        updateIngredientsInput();
    }
};