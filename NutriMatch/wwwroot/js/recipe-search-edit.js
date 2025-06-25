// Recipe Search JavaScript Functionality

// Initialize variables
let selectedIngredients = [];
let selectedInstructions = [];
let searchTimeout;
let currentFocus = -1;
let selectedIngredient = null;


// DOM Elements
const ingredientSearch = document.getElementById('ingredientSearch');
const ingredientDropdown = document.getElementById('ingredientDropdown');
const ingredientsList = document.getElementById('ingredientsList');
const hiddenIngredientsInput = document.getElementById('selectedIngredients');
const addButton = document.getElementById('addIngredientButton');
const qtyInput = document.getElementById('ingredientQuantity');
const unitSelect = document.getElementById('ingredientUnit');

// Instructions elements
const instructionInput = document.getElementById('instructionInput');
const addInstructionButton = document.getElementById('addInstructionButton');
const instructionsList = document.getElementById('instructionsList');
const hiddenInstructionsInput = document.getElementById('selectedInstructions');

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    initializeSearchFunctionality();
    initializeInstructionsFunctionality();
    var strr = JSON.parse((document.getElementById('selectedInstructionsScript').innerText))[0];
    strr = JSON.parse(strr);
    
    var strr2 = JSON.parse((document.getElementById('selectedIngredientsScript').innerText));
    console.log(strr2)

    strr2.forEach(element => {
        selectedIngredients.push({
            Id: element.IngredientId,
            Name: element.Ingredient.Name,
            Quantity: element.Quantity,
            Unit: element.Unit
        })
    });

    updateIngredientsDisplay();
    updateIngredientsInput();
    
    strr.forEach(element => {
        selectedInstructions.push(element);
    });
    updateInstructionsDisplay()
    updateInstructionsInput()

});

function initializeSearchFunctionality() {
    // Add ingredient button event listener
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

    // Ingredient search event listeners
    if (ingredientSearch) {
        ingredientSearch.addEventListener('input', function() {
            const query = this.value.trim();
            currentFocus = -1;
            selectedIngredient = null; // Reset selected ingredient when typing
            
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

    // Hide dropdowns when clicking outside
    document.addEventListener('click', function(e) {
        if (!e.target.closest('.search-container')) {
            hideDropdown(ingredientDropdown);
        }
    });
}

function initializeInstructionsFunctionality() {
    // Add instruction button event listener
    if (addInstructionButton) {
        addInstructionButton.addEventListener('click', function() {
            addInstruction();
        });
    }

    // Add instruction on Enter key press
    if (instructionInput) {
        instructionInput.addEventListener('keydown', function(e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                addInstruction();
            }
        });
    }
}

// Search ingredients function
async function searchIngredients(query) {
    if (!ingredientDropdown) return;

    try {
        ingredientDropdown.innerHTML = '<div class="loading">Loading...</div>';
        showDropdown(ingredientDropdown);

        // Fixed: Use GET method and correct endpoint
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

// Display search suggestions
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
        
        // Handle both string and object responses
        const name = typeof suggestion === 'string' ? suggestion : suggestion.name;
        const id = typeof suggestion === 'object' ? suggestion.id : null;
        
        // Highlight matching text
        const regex = new RegExp(`(${escapeRegExp(query)})`, 'gi');
        const highlightedText = name.replace(regex, '<strong>$1</strong>');
        item.innerHTML = highlightedText;
        
        // Add click event listener
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

// Handle keyboard navigation
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

// Set active item for keyboard navigation
function setActive(items) {
    // Remove highlighting from all items
    items.forEach(item => item.classList.remove('highlighted'));
    
    // Highlight current item
    if (currentFocus >= 0 && currentFocus < items.length) {
        items[currentFocus].classList.add('highlighted');
        items[currentFocus].scrollIntoView({ block: 'nearest' });
    }
}

// Add ingredient to selection
function addIngredient(ingredient, quantity, unit) {
    if (!ingredient || !ingredient.name) {
        console.error('Invalid ingredient data');
        return;
    }

    // Check if ingredient already exists
    const existingIngredient = selectedIngredients.find(item => item.Id === ingredient.id);
    
    if (existingIngredient) {
        alert('This ingredient is already added to the recipe.');
        return;
    }

    // Add new ingredient
    const newIngredient = {
        Id: ingredient.id,
        Name: ingredient.name,
        Quantity: quantity,
        Unit: unit
    };

    selectedIngredients.push(newIngredient);
    updateIngredientsDisplay();
    updateIngredientsInput();

    // Clear form fields
    if (ingredientSearch) ingredientSearch.value = '';
    if (qtyInput) qtyInput.value = '';
    if (unitSelect) unitSelect.value = '';
    
    selectedIngredient = null;
    hideDropdown(ingredientDropdown);
    currentFocus = -1;
}

// Remove ingredient from selection
function removeIngredient(ingredientName) {
    selectedIngredients = selectedIngredients.filter(item => item.Name !== ingredientName);
    updateIngredientsDisplay();
    updateIngredientsInput();
}

// Add instruction to selection
function addInstruction() {
    if (!instructionInput) return;

    const instruction = instructionInput.value.trim();
    
    if (instruction === '') {
        alert('Please enter an instruction.');
        return;
    }

    // Check if instruction already exists
    if (selectedInstructions.includes(instruction)) {
        alert('This instruction is already added.');
        return;
    }

    selectedInstructions.push(instruction);
    updateInstructionsDisplay();
    updateInstructionsInput();

    // Clear input field
    instructionInput.value = '';
}

// Remove instruction from selection
function removeInstruction(instructionIndex) {
    selectedInstructions.splice(instructionIndex, 1);
    updateInstructionsDisplay();
    updateInstructionsInput();
}

// Update ingredients display
function updateIngredientsDisplay() {
    if (!ingredientsList) return;

    if (selectedIngredients.length === 0) {
        ingredientsList.innerHTML = '<small class="text-muted">Selected ingredients will appear here</small>';
    } else {
        ingredientsList.innerHTML = selectedIngredients.map(ingredient => 
            `<span class="ingredient-tag" style="display: inline-block; background-color: #e3f2fd; padding: 5px 10px; margin: 2px; border-radius: 15px; font-size: 14px;">
                ${escapeHtml(ingredient.Quantity)} ${escapeHtml(ingredient.Unit)} of ${escapeHtml(ingredient.Name)}
                <span class="remove" onclick="removeIngredient('${escapeHtml(ingredient.Name)}')" 
                      style="cursor: pointer; margin-left: 8px; color: #dc3545; font-weight: bold;" 
                      title="Remove ingredient">&times;</span>
            </span>`
        ).join('');
    }
}

// Update instructions display
function updateInstructionsDisplay() {
    if (!instructionsList) return;

    if (selectedInstructions.length === 0) {
        instructionsList.innerHTML = '<small class="text-muted">Added instructions will appear here</small>';
    } else {
        instructionsList.innerHTML = selectedInstructions.map((instruction, index) => 
            `<div class="instruction-item" style="display: flex; flex-wrap: wrap; align-items: flex-start; background-color: #f8f9fa; padding: 10px; margin: 5px 0; border-radius: 8px; border-left: 4px solid #10b981;">
                <span class="instruction-number" style="background-color: #10b981; color: white; border-radius: 50%; width: 24px; height: 24px; display: flex; align-items: center; justify-content: center; font-size: 12px; font-weight: bold; margin-right: 10px; flex-shrink: 0;">${index + 1}</span>
                <span class="instruction-text" style="flex: 1; min-width: 0; word-wrap: break-word; overflow-wrap: break-word; line-height: 1.4;">
${escapeHtml(instruction)}</span>
                <span class="remove" onclick="removeInstruction(${index})" 
                      style="cursor: pointer; margin-left: 10px; color: #dc3545; font-weight: bold; font-size: 18px;" 
                      title="Remove instruction">&times;</span>
            </div>`
        ).join('');
    }
}

// Update hidden input with selected ingredients
function updateIngredientsInput() {
    if (hiddenIngredientsInput) {
        hiddenIngredientsInput.value = JSON.stringify(selectedIngredients);
        console.log('Updated hidden ingredients input:', hiddenIngredientsInput.value);
    }
}

// Update hidden input with selected instructions
function updateInstructionsInput() {
    if (hiddenInstructionsInput) {
        hiddenInstructionsInput.value = JSON.stringify(selectedInstructions);
        console.log('Updated hidden instructions input:', hiddenInstructionsInput.value);
    }
}

// Show dropdown
function showDropdown(dropdownElement) {
    if (dropdownElement) {
        dropdownElement.style.display = 'block';
    }
}

// Hide dropdown
function hideDropdown(dropdownElement) {
    if (dropdownElement) {
        dropdownElement.style.display = 'none';
    }
    currentFocus = -1;
}

// Utility function to escape HTML
function escapeHtml(text) {
    if (text === null || text === undefined) return '';
    const div = document.createElement('div');
    div.textContent = text.toString();
    return div.innerHTML;
}

// Utility function to escape RegExp special characters
function escapeRegExp(string) {
    return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}