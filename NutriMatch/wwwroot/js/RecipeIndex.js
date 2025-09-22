let currentPage = window.recipeData?.currentPage || 1;
let hasMorePages = window.recipeData?.hasMorePages || false;
let isLoading = false;
let showingFavoritesOnly = false;
let allLoadedRecipes = [];

document.addEventListener("DOMContentLoaded", function () {
  const searchInput = document.getElementById("searchInput");

  searchInput.addEventListener("input", function () {
    filterRecipes();
  });

  searchInput.addEventListener("keypress", function (e) {
    if (e.key === "Enter") {
      filterRecipes();
    }
  });

  initializeInfiniteScroll();

  filterRecipes();
});

function initializeInfiniteScroll() {
  window.addEventListener("scroll", function () {
    if (isLoading || !hasMorePages) return;

    const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
    const windowHeight = window.innerHeight;
    const documentHeight = document.documentElement.scrollHeight;

    if (scrollTop + windowHeight >= documentHeight - 600) {
      loadMoreRecipes();
    }
  });
}

async function loadMoreRecipes() {
  if (isLoading || !hasMorePages) return;

  isLoading = true;
  const loadingSpinner = document.getElementById("loadingSpinner");
  loadingSpinner.style.display = "block";

  try {
    const response = await fetch(
      `/Recipes/Index?page=${currentPage + 1}&pageSize=6`,
      {
        method: "GET",
        headers: {
          "X-Requested-With": "XMLHttpRequest",
        },
      }
    );

    if (!response.ok) {
      throw new Error("Network response was not ok");
    }

    const data = await response.json();

    if (data.recipes && data.recipes.length > 0) {
      appendRecipesToGrid(data.recipes);
      currentPage = data.currentPage;
      hasMorePages = data.hasMorePages;

      document.getElementById("totalCount").textContent = data.totalRecipes;

      if (!hasMorePages) {
        document.getElementById("endOfResults").style.display = "block";
      }
    }
  } catch (error) {
    console.error("Error loading more recipes:", error);
    showToast("Failed to load more recipes", "error");
  } finally {
    isLoading = false;
    loadingSpinner.style.display = "none";
  }
}

function appendRecipesToGrid(recipes) {
  const recipeGrid = document.getElementById("recipeGrid");

  recipes.forEach((recipe) => {
    allLoadedRecipes.push(recipe);

    const recipeCard = createRecipeCard(recipe);
    recipeGrid.appendChild(recipeCard);
  });

  filterRecipes();
}

function createRecipeCard(recipe) {
  const card = document.createElement("div");
  card.className = "recipe-card";
  card.setAttribute("data-calories", recipe.calories);
  card.setAttribute("data-protein", recipe.protein);
  card.setAttribute("data-carbs", recipe.carbs);
  card.setAttribute("data-fat", recipe.fat);
  card.onclick = () => showRecipeDetails(recipe.id, false);

  const truncatedTitle =
    recipe.title.length > 30
      ? recipe.title.substring(0, 30) + "…"
      : recipe.title;

  const truncatedUserName =
    recipe.userName.length > 23
      ? recipe.userName.substring(0, 23) + "…"
      : recipe.userName;

  card.innerHTML = `
        ${
          !recipe.isOwner
            ? `
            <button class="favorite-btn"
                onclick="event.stopPropagation(); toggleFavoriteFromIndex(this, ${
                  recipe.id
                })"
                data-recipe-id="${recipe.id}" data-favorited="${
                recipe.isFavorited
              }">
                <i class="${recipe.isFavorited ? "fas" : "far"} fa-heart"></i>
            </button>
        `
            : ""
        }
        <img src="${recipe.imageUrl}" alt="${
    recipe.title
  }" class="recipe-image">
        <div class="recipe-content">
            <h3 class="recipe-title">${truncatedTitle}</h3>
            <div class="recipe-meta">
                <span class="rating">
                    <i class="fas fa-star"></i> ${recipe.rating}
                </span>
                <span>
                    <i class="fas fa-user"></i>
                    ${truncatedUserName}
                </span>
                <span><i class="fas fa-calendar"> </i> ${
                  recipe.createdAt
                }</span>
            </div>
            <div class="recipe-macros">
                <div class="macro-item">
                    <div class="macro-value">${recipe.calories}</div>
                    <div class="macro-label">Cal</div>
                </div>
                <div class="macro-item">
                    <div class="macro-value">${recipe.protein}</div>
                    <div class="macro-label">Protein</div>
                </div>
                <div class="macro-item">
                    <div class="macro-value">${recipe.carbs}</div>
                    <div class="macro-label">Carbs</div>
                </div>
                <div class="macro-item">
                    <div class="macro-value">${recipe.fat}</div>
                    <div class="macro-label">Fats</div>
                </div>
            </div>
        </div>
    `;

  return card;
}

function showRecipeDetails(recipeId) {
  const clickedCard = event.currentTarget;
  clickedCard.classList.add("loading");

  const params = new URLSearchParams({
    isOwner: false,
    recipeDetailsDisplayContorol: "Index",
  });

  fetch(`/Recipes/Details/${recipeId}?${params}`)
    .then((response) => {
      if (!response.ok) {
        throw new Error("Network response was not ok");
      }
      return response.text();
    })
    .then((html) => {
      const modalContainer = document.getElementById("modalWindow");
      modalContainer.innerHTML = html;

      const scripts = modalContainer.querySelectorAll("script");
      scripts.forEach((script) => {
        const newScript = document.createElement("script");
        if (script.src) {
          newScript.src = script.src;
        } else {
          newScript.textContent = script.textContent;
        }
        document.body.appendChild(newScript);
        document.body.removeChild(newScript);
      });

      const modalElement = modalContainer.querySelector(".modal");
      if (modalElement) {
        const modal = new bootstrap.Modal(modalElement);
        modal.show();

        modalElement.addEventListener("hidden.bs.modal", function () {
          modalContainer.innerHTML = "";
          clickedCard.classList.remove("loading");
        });

        modalElement.addEventListener("shown.bs.modal", function () {
          clickedCard.classList.remove("loading");
        });
      } else {
        clickedCard.classList.remove("loading");
      }
    })
    .catch((err) => {
      console.error("Failed to fetch recipe details", err);
      alert("Failed to load recipe details. Please try again.");
      clickedCard.classList.remove("loading");
    });
}

function updateSlider(type) {
  const minSlider = document.getElementById(type + "Min");
  const maxSlider = document.getElementById(type + "Max");
  const valueDisplay = document.getElementById(type + "Value");
  const fill = document.getElementById(type + "Fill");

  let minVal = parseInt(minSlider.value);
  let maxVal = parseInt(maxSlider.value);

  if (minVal > maxVal) {
    if (event.target === minSlider) {
      maxSlider.value = minVal;
      maxVal = minVal;
    } else {
      minSlider.value = maxVal;
      minVal = maxVal;
    }
  }

  valueDisplay.textContent = minVal + " - " + maxVal;

  const min = parseInt(minSlider.min);
  const max = parseInt(minSlider.max);
  const range = max - min;

  const leftPercent = ((minVal - min) / range) * 100;
  const rightPercent = ((maxVal - min) / range) * 100;

  fill.style.left = leftPercent + "%";
  fill.style.width = rightPercent - leftPercent + "%";

  filterRecipes();
}

function filterRecipes() {
  const calories = {
    min: parseInt(document.getElementById("caloriesMin").value),
    max: parseInt(document.getElementById("caloriesMax").value),
  };
  const protein = {
    min: parseInt(document.getElementById("proteinMin").value),
    max: parseInt(document.getElementById("proteinMax").value),
  };
  const carbs = {
    min: parseInt(document.getElementById("carbsMin").value),
    max: parseInt(document.getElementById("carbsMax").value),
  };
  const fats = {
    min: parseInt(document.getElementById("fatsMin").value),
    max: parseInt(document.getElementById("fatsMax").value),
  };

  const searchTerm = document.getElementById("searchInput").value.toLowerCase();

  const recipeCards = document.querySelectorAll(".recipe-card");
  let visibleCount = 0;

  recipeCards.forEach((card) => {
    const title = card.querySelector(".recipe-title").textContent.toLowerCase();

    const recipeCalories = parseInt(card.dataset.calories) || 0;
    const recipeProtein = parseInt(card.dataset.protein) || 0;
    const recipeCarbs = parseInt(card.dataset.carbs) || 0;
    const recipeFats = parseInt(card.dataset.fat) || 0;

    const favoriteButton = card.querySelector(".favorite-btn");
    const isFavorited = favoriteButton
      ? favoriteButton.getAttribute("data-favorited") === "true"
      : false;

    const matchesSearch = searchTerm === "" || title.includes(searchTerm);

    const matchesMacros =
      recipeCalories >= calories.min &&
      recipeCalories <= calories.max &&
      recipeProtein >= protein.min &&
      recipeProtein <= protein.max &&
      recipeCarbs >= carbs.min &&
      recipeCarbs <= carbs.max &&
      recipeFats >= fats.min &&
      recipeFats <= fats.max;

    const matchesFavorites = !showingFavoritesOnly || isFavorited;

    if (matchesSearch && matchesMacros && matchesFavorites) {
      card.style.display = "block";
      visibleCount++;
    } else {
      card.style.display = "none";
    }
  });

  document.getElementById("visibleCount").textContent = visibleCount;
}

function resetFilters() {
  document.getElementById("caloriesMin").value = 0;
  document.getElementById("caloriesMax").value = 2000;
  document.getElementById("proteinMin").value = 0;
  document.getElementById("proteinMax").value = 150;
  document.getElementById("carbsMin").value = 0;
  document.getElementById("carbsMax").value = 150;
  document.getElementById("fatsMin").value = 0;
  document.getElementById("fatsMax").value = 150;
  document.getElementById("searchInput").value = "";

  if (showingFavoritesOnly) {
    toggleFavoritesFilter();
  }

  updateSlider("calories");
  updateSlider("protein");
  updateSlider("carbs");
  updateSlider("fats");

  filterRecipes();
}

function openDeleteModal(recipeId, isOwner) {
  const deleteButton = event.target.closest("button");
  deleteButton.classList.add("loading");

  const recipeModalContainer = document.getElementById("modalWindow");
  const recipeModalElement = recipeModalContainer.querySelector(".modal");
  const savedRecipeHtml = recipeModalContainer.innerHTML;

  let recipeModalWasOpen = false;
  if (recipeModalElement && recipeModalElement.classList.contains("show")) {
    const recipeModalInstance = bootstrap.Modal.getInstance(recipeModalElement);
    if (recipeModalInstance) {
      recipeModalInstance.hide();
      recipeModalWasOpen = true;
    }
  }

  fetch(`/Recipes/Details/${recipeId}/${isOwner}`)
    .then((response) => response.text())
    .then((html) => {
      const deleteModalContainer = document.getElementById("modalWindowDelete");
      deleteModalContainer.innerHTML = html;

      const deleteModalElement = deleteModalContainer.querySelector(".modal");
      if (deleteModalElement) {
        const deleteModal = new bootstrap.Modal(deleteModalElement);
        deleteModal.show();

        deleteModalElement.addEventListener("hidden.bs.modal", function () {
          deleteButton.classList.remove("loading");
          deleteModalContainer.innerHTML = "";

          if (recipeModalWasOpen && savedRecipeHtml.trim() !== "") {
            recipeModalContainer.innerHTML = savedRecipeHtml;
            const restoredModal = recipeModalContainer.querySelector(".modal");
            if (restoredModal) {
              const restoredInstance = new bootstrap.Modal(restoredModal);
              restoredInstance.show();
            }
          }
        });

        deleteModalElement.addEventListener("shown.bs.modal", function () {
          deleteButton.classList.remove("loading");
        });
      } else {
        deleteButton.classList.remove("loading");
      }
    })
    .catch((error) => {
      console.error("Error loading delete modal:", error);
      deleteButton.classList.remove("loading");
      location.href = `/Recipes/Delete/${recipeId}`;
    });
}

async function toggleFavoriteFromIndex(button, recipeId) {
  try {
    const token = document.querySelector(
      'input[name="__RequestVerificationToken"]'
    )?.value;

    const response = await fetch("/Recipes/ToggleFavorite", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        RequestVerificationToken: token,
      },
      body: JSON.stringify({ recipeId: recipeId }),
    });

    const result = await response.json();

    if (result.success) {
      const heartIcon = button.querySelector("i");
      const isFavorited = result.isFavorited;

      if (isFavorited) {
        heartIcon.classList.remove("far");
        heartIcon.classList.add("fas");
        button.setAttribute("data-favorited", "true");
      } else {
        heartIcon.classList.remove("fas");
        heartIcon.classList.add("far");
        button.setAttribute("data-favorited", "false");
      }

      if (showingFavoritesOnly) {
        setTimeout(() => filterRecipes(), 100);
      }

      showToast(result.message, "success");
    } else {
      showToast(result.message || "Failed to update favorite", "error");
    }
  } catch (error) {
    console.error("Error toggling favorite:", error);
    showToast("An error occurred while updating favorites", "error");
  }
}

function showToast(message, type = "info") {
  const toast = document.createElement("div");
  toast.className = `toast toast-${type}`;
  toast.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        padding: 12px 20px;
        border-radius: 4px;
        color: white;
        font-weight: 500;
        z-index: 10000;
        opacity: 0;
        transition: opacity 0.3s ease;
    `;

  const colors = {
    success: "#10b981",
    error: "#ef4444",
    info: "#3b82f6",
  };
  toast.style.backgroundColor = colors[type] || colors.info;

  toast.textContent = message;
  document.body.appendChild(toast);

  setTimeout(() => (toast.style.opacity = "1"), 100);

  setTimeout(() => {
    toast.style.opacity = "0";
    setTimeout(() => document.body.removeChild(toast), 300);
  }, 3000);
}

function toggleFavoritesFilter() {
  const button = document.getElementById("favoritesToggle");

  showingFavoritesOnly = !showingFavoritesOnly;

  if (showingFavoritesOnly) {
    button.innerHTML = '<i class="fas fa-heart me-2"></i>Show All Recipes';
    button.className = "btn btn-success w-100";
  } else {
    button.innerHTML = '<i class="fas fa-heart me-2"></i>Show Favorites Only';
    button.className = "btn btn-outline-success w-100";
  }

  filterRecipes();
}
