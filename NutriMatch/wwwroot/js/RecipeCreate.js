let selectedIngredients = [];
let selectedInstructions = [];
let searchTimeout;
let currentFocus = -1;
let selectedIngredient = null;

const ingredientSearch = document.getElementById("ingredientSearch");
const ingredientDropdown = document.getElementById("ingredientDropdown");
const ingredientsList = document.getElementById("ingredientsList");
const hiddenIngredientsInput = document.getElementById("selectedIngredients");
const addButton = document.getElementById("addIngredientButton");
const qtyInput = document.getElementById("ingredientQuantity");
const unitSelect = document.getElementById("ingredientUnit");

const instructionInput = document.getElementById("instructionInput");
const addInstructionButton = document.getElementById("addInstructionButton");
const instructionsList = document.getElementById("instructionsList");
const hiddenInstructionsInput = document.getElementById("selectedInstructions");

const fileUploadArea = document.getElementById("fileUploadArea");
const fileInput = document.getElementById("RecipeImage");
const imagePreview = document.getElementById("imagePreview");

const addNewIngredientBtn = document.getElementById("addNewIngredientBtn");
const addIngredientModal = document.getElementById("addIngredientModal");
const addIngredientForm = document.getElementById("addIngredientForm");
const closeModal = document.getElementById("closeModal");
const cancelAddIngredient = document.getElementById("cancelAddIngredient");
let lastSearchQuery = "";

document.addEventListener("DOMContentLoaded", function () {
  initializeSearchFunctionality();
  initializeInstructionsFunctionality();
  initializeFileUpload();
  initializeModalFunctionality();
});

function initializeFileUpload() {
  if (fileUploadArea) {
    fileUploadArea.addEventListener("click", () => fileInput.click());

    fileUploadArea.addEventListener("dragover", (e) => {
      e.preventDefault();
      fileUploadArea.classList.add("dragover");
    });

    fileUploadArea.addEventListener("dragleave", () => {
      fileUploadArea.classList.remove("dragover");
    });

    fileUploadArea.addEventListener("drop", (e) => {
      e.preventDefault();
      fileUploadArea.classList.remove("dragover");
      const files = e.dataTransfer.files;
      if (files.length > 0) {
        handleFileSelect(files[0]);
        setFileToInput(files[0]);
      }
    });

    fileInput.addEventListener("change", (e) => {
      if (e.target.files && e.target.files[0]) {
        handleFileSelect(e.target.files[0]);
      }
    });
  }
}

function setFileToInput(file) {
  const dataTransfer = new DataTransfer();
  dataTransfer.items.add(file);

  fileInput.files = dataTransfer.files;
}

function initializeSearchFunctionality() {
  if (addButton) {
    addButton.addEventListener("click", function () {
      if (selectedIngredient && qtyInput && unitSelect) {
        const qty = parseFloat(qtyInput.value);
        const unit = unitSelect.value;

        if (qty > 0 && unit && selectedIngredient.name) {
          addIngredient(selectedIngredient, qty, unit);
        } else {
          alert("Please enter a valid quantity and select a unit.");
        }
      } else {
        alert("Please search and select an ingredient first.");
      }
    });
  }

  if (ingredientSearch) {
    ingredientSearch.addEventListener("input", function () {
      const query = this.value.trim();
      currentFocus = -1;
      selectedIngredient = null;

      if (query === "") {
        hideDropdown(ingredientDropdown);
        if (addNewIngredientBtn) {
          addNewIngredientBtn.style.display = "none";
        }
        return;
      }

      clearTimeout(searchTimeout);
      searchTimeout = setTimeout(() => {
        searchIngredients(query);
      }, 300);
    });

    ingredientSearch.addEventListener("keydown", function (e) {
      handleKeyNavigation(e, ingredientDropdown);
    });

    ingredientSearch.addEventListener("focus", function () {
      if (this.value.trim() !== "") {
        searchIngredients(this.value.trim());
      }
    });
  }

  document.addEventListener("click", function (e) {
    if (!e.target.closest(".search-container")) {
      hideDropdown(ingredientDropdown);
    }
  });
}

function initializeInstructionsFunctionality() {
  if (addInstructionButton) {
    addInstructionButton.addEventListener("click", function () {
      addInstruction();
    });
  }

  if (instructionInput) {
    instructionInput.addEventListener("keydown", function (e) {
      if (e.key === "Enter") {
        e.preventDefault();
        addInstruction();
      }
    });
  }
}

async function searchIngredients(query) {
  if (!ingredientDropdown) return;

  try {
    ingredientDropdown.innerHTML = '<div class="loading">Loading...</div>';
    showDropdown(ingredientDropdown);

    const response = await fetch(
      `/Recipes/getSuggestions?query=${encodeURIComponent(query)}`,
      {
        method: "GET",
      }
    );

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const suggestions = await response.json();
    displaySuggestions(suggestions, query);
  } catch (error) {
    console.error("Error fetching suggestions:", error);
    ingredientDropdown.innerHTML =
      '<div class="no-results">Error loading suggestions. Please try again.</div>';
    showDropdown(ingredientDropdown);
  }
}

function displaySuggestions(suggestions, query) {
  if (!ingredientDropdown) return;

  lastSearchQuery = query;
  ingredientDropdown.innerHTML = "";

  if (!suggestions || suggestions.length === 0) {
    ingredientDropdown.innerHTML =
      '<div class="no-results">No results found</div>';
    showDropdown(ingredientDropdown);

    if (addNewIngredientBtn) {
      addNewIngredientBtn.style.display = "block";
      console.log("Showing button for query:", query);
    }
    return;
  }

  if (addNewIngredientBtn) {
    addNewIngredientBtn.style.display = "none";
  }

  suggestions.forEach((suggestion, index) => {
    const item = document.createElement("div");
    item.className = "dropdown-item";
    item.setAttribute("data-index", index);

    const name = suggestion.name;
    const id = suggestion.id;

    const regex = new RegExp(`(${query})`, "gi");
    const highlightedText = name.replace(regex, "<strong>$1</strong>");
    item.innerHTML = highlightedText;

    item.addEventListener("click", function () {
      selectedIngredient = {
        id: id,
        name: name,
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

  const items = dropdownElement.querySelectorAll(
    ".dropdown-item:not(.no-results):not(.loading)"
  );

  switch (e.key) {
    case "ArrowDown":
      e.preventDefault();
      currentFocus++;
      if (currentFocus >= items.length) currentFocus = 0;
      setActive(items);
      break;

    case "ArrowUp":
      e.preventDefault();
      currentFocus--;
      if (currentFocus < 0) currentFocus = items.length - 1;
      setActive(items);
      break;

    case "Enter":
      e.preventDefault();
      if (currentFocus > -1 && items[currentFocus]) {
        items[currentFocus].click();
      }
      break;

    case "Escape":
      hideDropdown(dropdownElement);
      e.target.blur();
      break;
  }
}

function setActive(items) {
  items.forEach((item) => item.classList.remove("highlighted"));

  if (currentFocus >= 0 && currentFocus < items.length) {
    items[currentFocus].classList.add("highlighted");
    items[currentFocus].scrollIntoView({ block: "nearest" });
  }
}

function addIngredient(ingredient, quantity, unit) {
  if (!ingredient || !ingredient.name) {
    console.error("Invalid ingredient data");
    return;
  }

  const existingIngredient = selectedIngredients.find(
    (item) => item.Id === ingredient.id
  );

  if (existingIngredient) {
    alert("This ingredient is already added to the recipe.");
    return;
  }

  const newIngredient = {
    Id: ingredient.id,
    Name: ingredient.name,
    Quantity: quantity,
    Unit: unit,
  };

  selectedIngredients.push(newIngredient);
  updateIngredientsDisplay();
  updateIngredientsInput();

  if (ingredientSearch) ingredientSearch.value = "";
  if (qtyInput) qtyInput.value = "";
  if (unitSelect) unitSelect.value = "";

  selectedIngredient = null;
  hideDropdown(ingredientDropdown);
  currentFocus = -1;
}

function removeIngredient(ingredientName) {
  selectedIngredients = selectedIngredients.filter(
    (item) => item.Name !== ingredientName
  );
  updateIngredientsDisplay();
  updateIngredientsInput();
}

function addInstruction() {
  if (!instructionInput) return;

  const instruction = instructionInput.value.trim();

  if (instruction === "") {
    alert("Please enter an instruction.");
    return;
  }

  if (selectedInstructions.includes(instruction)) {
    alert("This instruction is already added.");
    return;
  }

  selectedInstructions.push(instruction);
  updateInstructionsDisplay();
  updateInstructionsInput();

  instructionInput.value = "";
}

function removeInstruction(instructionIndex) {
  selectedInstructions.splice(instructionIndex, 1);
  updateInstructionsDisplay();
  updateInstructionsInput();
}

function updateIngredientsDisplay() {
  if (!ingredientsList) return;

  if (selectedIngredients.length === 0) {
    ingredientsList.innerHTML =
      '<small class="text-muted">Selected ingredients will appear here</small>';
  } else {
    ingredientsList.innerHTML = selectedIngredients
      .map(
        (ingredient) =>
          `<span class="ingredient-tag">
                ${ingredient.Quantity} ${ingredient.Unit} of ${ingredient.Name}
                <span class="remove" onclick="removeIngredient('${ingredient.Name}')" 
                    title="Remove ingredient">&times;</span>
            </span>`
      )
      .join("");
  }
}

function updateInstructionsDisplay() {
  if (!instructionsList) return;

  if (selectedInstructions.length === 0) {
    instructionsList.innerHTML =
      '<small class="text-muted">Added instructions will appear here</small>';
  } else {
    instructionsList.innerHTML = selectedInstructions
      .map(
        (instruction, index) =>
          `<div class="instruction-item">
                <span class="instruction-number">${index + 1}</span>
                <span class="instruction-text">${instruction}</span>
                <span class="remove" onclick="removeInstruction(${index})" style="font-size: 18px;" title="Remove instruction">&times;</span>
            </div>`
      )
      .join("");
  }
}

function updateIngredientsInput() {
  if (hiddenIngredientsInput) {
    hiddenIngredientsInput.value = JSON.stringify(selectedIngredients);
  }
}

function updateInstructionsInput() {
  if (hiddenInstructionsInput) {
    hiddenInstructionsInput.value = JSON.stringify(selectedInstructions);
  }
}

function showDropdown(dropdownElement) {
  if (dropdownElement) {
    dropdownElement.style.display = "block";
  }
}

function hideDropdown(dropdownElement) {
  if (dropdownElement) {
    dropdownElement.style.display = "none";
  }
  if (addNewIngredientBtn) {
    addNewIngredientBtn.style.display = "none";
  }
  currentFocus = -1;
}

function handleFileSelect(file) {
  if (file.type.startsWith("image/")) {
    const reader = new FileReader();
    reader.onload = (e) => {
      imagePreview.innerHTML = `
                <img id="image-preview" src="${e.target.result}" alt="Recipe preview">
                <p style="margin-top: 0.5rem; color: #4CAF50; font-weight: 500;">✓ Image uploaded successfully</p>
            `;
    };
    reader.readAsDataURL(file);
  }
}

function validateInstructions() {
  const instructionsContainer = document.getElementById("instructionsList");
  const instructionsInput = document.getElementById("selectedInstructions");
  const hasInstructions =
    instructionsInput.value &&
    instructionsInput.value !== "[]" &&
    instructionsInput.value.trim() !== "";

  if (!hasInstructions) {
    instructionsContainer.innerHTML =
      '<div class="items-empty error">Please add at least one instruction step</div>';
    alert("Please fill in the instructions");
    return false;
  }
  return true;
}

document.querySelector("form").addEventListener("submit", function (e) {
  if (!validateInstructions()) {
    e.preventDefault();
    return false;
  }
});

function initializeModalFunctionality() {
  console.log("Initializing modal functionality");

  const addNewIngredientBtn = document.getElementById("addNewIngredientBtn");
  const addIngredientModal = document.getElementById("addIngredientModal");
  const addIngredientForm = document.getElementById("addIngredientForm");
  const closeModal = document.getElementById("closeModal");
  const cancelAddIngredient = document.getElementById("cancelAddIngredient");

  if (addNewIngredientBtn) {
    addNewIngredientBtn.addEventListener("click", function (e) {
      e.preventDefault();

      const qty = qtyInput ? parseFloat(qtyInput.value) : 0;
      const unit = unitSelect ? unitSelect.value : "";

      if (!qty || qty <= 0 || !unit) {
        showValidationMessage(
          "Please enter a valid quantity and select a unit before adding a new ingredient."
        );
        return;
      }

      showAddIngredientModal(lastSearchQuery);
    });
  }

  if (closeModal) {
    closeModal.addEventListener("click", hideAddIngredientModal);
  }

  if (cancelAddIngredient) {
    cancelAddIngredient.addEventListener("click", function (e) {
      e.preventDefault();
      hideAddIngredientModal();
    });
  }

  if (addIngredientModal) {
    addIngredientModal.addEventListener("click", function (e) {
      if (e.target === addIngredientModal) {
        hideAddIngredientModal();
      }
    });

    document.addEventListener("keydown", function (e) {
      if (e.key === "Escape" && addIngredientModal.style.display === "flex") {
        hideAddIngredientModal();
      }
    });
  }

  if (addIngredientForm) {
    addIngredientForm.addEventListener("submit", handleAddNewIngredient);
  }
}

function showAddIngredientModal(ingredientName = "") {
  const addIngredientModal = document.getElementById("addIngredientModal");

  if (addIngredientModal) {
    const nameInput = document.getElementById("newIngredientName");
    if (nameInput) {
      nameInput.value = ingredientName;
    }

    addIngredientModal.style.display = "flex";
    addIngredientModal.classList.add("show");

    setTimeout(() => {
      if (nameInput) {
        nameInput.focus();
        nameInput.select();
      }
    }, 100);

    hideDropdown(ingredientDropdown);
  }
}

function hideAddIngredientModal() {
  const addIngredientModal = document.getElementById("addIngredientModal");

  if (addIngredientModal) {
    addIngredientModal.classList.remove("show");

    setTimeout(() => {
      addIngredientModal.style.display = "none";
    }, 300);

    const form = document.getElementById("addIngredientForm");
    if (form) {
      form.reset();
      const submitBtn = form.querySelector('button[type="submit"]');
      if (submitBtn) {
        submitBtn.disabled = false;
        submitBtn.classList.remove("loading");
      }
    }
  }
}

async function handleAddNewIngredient(e) {
  e.preventDefault();

  const form = e.target;
  const submitBtn = form.querySelector('button[type="submit"]');

  if (submitBtn) {
    submitBtn.disabled = true;
    submitBtn.classList.add("loading");
  }

  if (!document.getElementById("newIngredientName").value.trim()) {
    alert("Please enter an ingredient name");
    if (submitBtn) {
      submitBtn.disabled = false;
      submitBtn.classList.remove("loading");
    }
    return;
  }

  try {
    let token = document.querySelector(
      'input[name="__RequestVerificationToken"]'
    );
    if (!token) {
      token = document.querySelector('input[name="RequestVerificationToken"]');
    }
    if (!token) {
      token = form.querySelector('input[name="__RequestVerificationToken"]');
    }

    const tokenValue = token ? token.value : "";

    const response = await fetch("/Recipes/AddIngredient", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "X-Requested-With": "XMLHttpRequest",
        RequestVerificationToken: tokenValue,
      },
      body: JSON.stringify({
        Name: document.getElementById("newIngredientName").value.trim(),
        Calories:
          parseFloat(document.getElementById("newIngredientCalories").value) ||
          0.0,
        Protein:
          parseFloat(document.getElementById("newIngredientProtein").value) ||
          0.0,
        Carbs:
          parseFloat(document.getElementById("newIngredientCarbs").value) ||
          0.0,
        Fat:
          parseFloat(document.getElementById("newIngredientFat").value) || 0.0,
      }),
    });

    if (!response.ok) {
      const errorText = await response.text();
      console.error("Server response:", errorText);
      throw new Error(errorText || `HTTP error! status: ${response.status}`);
    }

    const newIngredient = await response.json();

    selectedIngredient = {
      id: newIngredient.id,
      name: newIngredient.name,
    };

    const ingredientSearch = document.getElementById("ingredientSearch");
    if (ingredientSearch) {
      ingredientSearch.value = newIngredient.name;
    }

    hideAddIngredientModal();

    const qtyInput = document.getElementById("ingredientQuantity");
    const unitSelect = document.getElementById("ingredientUnit");

    if (qtyInput && unitSelect && qtyInput.value && unitSelect.value) {
      const qty = parseFloat(qtyInput.value);
      const unit = unitSelect.value;

      if (qty > 0) {
        addIngredient(selectedIngredient, qty, unit);
      }
    }

    showSuccessMessage("Ingredient added successfully!");

    hideDropdown(ingredientDropdown);
  } catch (error) {
    console.error("Error adding ingredient:", error);

    let errorMessage = "Error adding ingredient. Please try again.";
    if (error.message.includes("already exists")) {
      errorMessage = "An ingredient with this name already exists.";
    } else if (error.message.includes("required")) {
      errorMessage = "Please fill in all required fields.";
    } else if (error.message.includes("Anti-forgery")) {
      errorMessage =
        "Security validation failed. Please refresh the page and try again.";
    }

    alert(errorMessage);
  } finally {
    if (submitBtn) {
      submitBtn.disabled = false;
      submitBtn.classList.remove("loading");
    }
  }
}

function showSuccessMessage(message) {
  const successMsg = document.createElement("div");
  successMsg.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        background: #10b981;
        color: white;
        padding: 1rem 1.5rem;
        border-radius: 8px;
        box-shadow: 0 4px 12px rgba(16, 185, 129, 0.3);
        z-index: 1001;
        font-weight: 500;
        animation: slideInRight 0.3s ease;
    `;
  successMsg.textContent = message;

  document.body.appendChild(successMsg);

  setTimeout(() => {
    successMsg.style.animation = "slideOutRight 0.3s ease forwards";
    setTimeout(() => {
      if (successMsg.parentNode) {
        successMsg.parentNode.removeChild(successMsg);
      }
    }, 300);
  }, 3000);
}

if (!document.getElementById("success-message-styles")) {
  const style = document.createElement("style");
  style.id = "success-message-styles";
  style.textContent = `
        @keyframes slideInRight {
            from {
                transform: translateX(100%);
                opacity: 0;
            }
            to {
                transform: translateX(0);
                opacity: 1;
            }
        }
        
        @keyframes slideOutRight {
            from {
                transform: translateX(0);
                opacity: 1;
            }
            to {
                transform: translateX(100%);
                opacity: 0;
            }
        }
    `;
  document.head.appendChild(style);
}

function showValidationMessage(message) {
  const existingMsg = document.getElementById("validation-message");
  if (existingMsg) {
    existingMsg.remove();
  }

  const validationMsg = document.createElement("div");
  validationMsg.id = "validation-message";
  validationMsg.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        background: #ef4444;
        color: white;
        padding: 1rem 1.5rem;
        border-radius: 8px;
        box-shadow: 0 4px 12px rgba(239, 68, 68, 0.3);
        z-index: 1001;
        font-weight: 500;
        animation: slideInRight 0.3s ease;
        max-width: 300px;
    `;
  validationMsg.textContent = message;

  document.body.appendChild(validationMsg);

  setTimeout(() => {
    validationMsg.style.animation = "slideOutRight 0.3s ease forwards";
    setTimeout(() => {
      if (validationMsg.parentNode) {
        validationMsg.parentNode.removeChild(validationMsg);
      }
    }, 300);
  }, 4000);
}
