document.addEventListener("DOMContentLoaded", function () {
  setupBulkActions();
  setupSearchFunctionality();
  setupSortingFunctionality();
  updateDisplayCount();
});

function setupBulkActions() {
  const selectAllCheckbox = document.getElementById("selectAll");
  const recipeCheckboxes = document.querySelectorAll(".recipe-checkbox");
  const bulkApproveBtn = document.getElementById("bulkApprove");

  selectAllCheckbox.addEventListener("change", function () {
    recipeCheckboxes.forEach((checkbox) => {
      checkbox.checked = this.checked;
      toggleRecipeSelection(checkbox);
    });
    updateBulkActionButtons();
  });

  recipeCheckboxes.forEach((checkbox) => {
    checkbox.addEventListener("change", function () {
      toggleRecipeSelection(this);
      updateBulkActionButtons();
      updateSelectAllState();
    });
  });

  bulkApproveBtn.addEventListener("click", handleBulkApprove);
}

function toggleRecipeSelection(checkbox) {
  const recipeCard = checkbox.closest(".recipe-card");
  if (checkbox.checked) {
    recipeCard.classList.add("selected");
  } else {
    recipeCard.classList.remove("selected");
  }
}

function updateBulkActionButtons() {
  const selectedCheckboxes = document.querySelectorAll(
    ".recipe-checkbox:checked"
  );
  const bulkApproveBtn = document.getElementById("bulkApprove");
  const bulkActionsSection = document.getElementById("bulkActionsSection");

  const hasSelections = selectedCheckboxes.length > 0;
  bulkApproveBtn.disabled = !hasSelections;

  if (hasSelections) {
    bulkActionsSection.classList.add("show");
  } else {
    bulkActionsSection.classList.remove("show");
  }
}

function updateSelectAllState() {
  const selectAllCheckbox = document.getElementById("selectAll");
  const recipeCheckboxes = document.querySelectorAll(".recipe-checkbox");
  const checkedBoxes = document.querySelectorAll(".recipe-checkbox:checked");

  if (checkedBoxes.length === 0) {
    selectAllCheckbox.indeterminate = false;
    selectAllCheckbox.checked = false;
  } else if (checkedBoxes.length === recipeCheckboxes.length) {
    selectAllCheckbox.indeterminate = false;
    selectAllCheckbox.checked = true;
  } else {
    selectAllCheckbox.indeterminate = true;
  }
}

function approveRecipe(recipeId) {
  showLoadingOverlay();

  const token = document.querySelector(
    'input[name="__RequestVerificationToken"]'
  ).value;

  fetch("/Admin/ApproveRecipe", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      RequestVerificationToken: token,
    },
    body: JSON.stringify({ recipeId: recipeId }),
  })
    .then((response) => response.json())
    .then((data) => {
      hideLoadingOverlay();
      if (data.success) {
        showSuccess("Recipe approved successfully!");
        removeRecipeCard(recipeId);
        hideRecipeDetails();
      } else {
        showError(data.message || "Failed to approve recipe");
      }
    })
    .catch((error) => {
      hideLoadingOverlay();
      console.error("Error:", error);
      showError("An error occurred while approving the recipe");
    });
}

function declineRecipe(recipeId) {
  fetch(`/Admin/DeclineReasonModel/${recipeId}`)
    .then((response) => {
      if (!response.ok) {
        throw new Error("Failed to load decline modal");
      }
      return response.text();
    })
    .then((html) => {
      const modalContainer = document.getElementById("declineModalContainer");
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

      hideRecipeDetails();

      const modalElement = modalContainer.querySelector("#recipeDeclineModal");
      if (modalElement) {
        const modal = new bootstrap.Modal(modalElement);
        modal.show();
      }
    })
    .catch((error) => {
      console.error("Error loading decline modal:", error);
      showError("Failed to load decline modal");
    });
}

function cancelDecline(recipeId) {
  const declineSection = document.getElementById(`declineReason_${recipeId}`);
  const recipeCard = declineSection.closest(".recipe-card");

  declineSection.style.display = "none";

  const actionButtons = recipeCard.querySelectorAll(
    ".admin-actions-buttons .btn"
  );
  actionButtons.forEach((btn) => {
    btn.style.display = "";
  });

  document.getElementById(`declineSelect_${recipeId}`).value = "";
  document.getElementById(`declineNotes_${recipeId}`).value = "";
}

function handleBulkApprove() {
  const selectedRecipes = getSelectedRecipeIds();

  if (selectedRecipes.length === 0) {
    showError("No recipes selected");
    return;
  }

  showLoadingOverlay();

  const token = document.querySelector(
    'input[name="__RequestVerificationToken"]'
  ).value;

  fetch("/Admin/BulkApproveRecipes", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      RequestVerificationToken: token,
    },
    body: JSON.stringify({ recipeIds: selectedRecipes }),
  })
    .then((response) => response.json())
    .then((data) => {
      hideLoadingOverlay();

      if (data.success) {
        showSuccess(`${data.approvedCount} recipe(s) approved successfully!`);
        selectedRecipes.forEach((recipeId) => removeRecipeCard(recipeId));
      } else {
        showError(data.message || "Failed to approve recipes");
      }
    })
    .catch((error) => {
      hideLoadingOverlay();
      console.error("Error:", error);
      showError("An error occurred during bulk approval");
    });
}

function getSelectedRecipeIds() {
  const selectedCheckboxes = document.querySelectorAll(
    ".recipe-checkbox:checked"
  );
  return Array.from(selectedCheckboxes).map((checkbox) =>
    parseInt(checkbox.dataset.recipeId)
  );
}

function setupSearchFunctionality() {
  const searchInput = document.getElementById("searchInput");
  let searchTimeout;

  searchInput.addEventListener("input", function () {
    clearTimeout(searchTimeout);
    searchTimeout = setTimeout(() => {
      filterRecipes();
    }, 300);
  });
}

function filterRecipes() {
  const searchTerm = document.getElementById("searchInput").value.toLowerCase();
  const recipeCards = document.querySelectorAll(".recipe-card");
  let visibleCount = 0;

  recipeCards.forEach((card) => {
    const title = card.querySelector(".recipe-title").textContent.toLowerCase();
    const author = card
      .querySelector(".recipe-meta span")
      .textContent.toLowerCase();

    const isVisible = title.includes(searchTerm) || author.includes(searchTerm);
    card.style.display = isVisible ? "block" : "none";

    if (isVisible) visibleCount++;
  });

  updateDisplayCount(visibleCount);
}

function setupSortingFunctionality() {
  const sortFilter = document.getElementById("sortFilter");
  sortFilter.addEventListener("change", function () {
    sortRecipes(this.value);
  });
}

function sortRecipes(sortBy) {
  const recipeGrid = document.getElementById("recipeGrid");
  const recipeCards = Array.from(recipeGrid.querySelectorAll(".recipe-card"));

  recipeCards.sort((a, b) => {
    switch (sortBy) {
      case "newest":
        return parseInt(b.dataset.recipeId) - parseInt(a.dataset.recipeId);
      case "oldest":
        return parseInt(a.dataset.recipeId) - parseInt(b.dataset.recipeId);
      case "author":
        const authorA = a
          .querySelector(".recipe-meta span")
          .textContent.toLowerCase();
        const authorB = b
          .querySelector(".recipe-meta span")
          .textContent.toLowerCase();
        return authorA.localeCompare(authorB);
      case "calories":
        return parseInt(a.dataset.calories) - parseInt(b.dataset.calories);
      default:
        return 0;
    }
  });

  recipeCards.forEach((card) => recipeGrid.appendChild(card));
}

function showRecipeDetails(recipeId, isAdmin = false, recipeControl = "") {
  currentRecipeId = recipeId;
  const params = new URLSearchParams({
    isOwner: true,
    recipeDetailsDisplayContorol: recipeControl,
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

      const existingScripts = document.querySelectorAll(
        "script[data-recipe-modal]"
      );
      existingScripts.forEach((script) => script.remove());

      const scripts = modalContainer.querySelectorAll("script");
      scripts.forEach((script) => {
        const newScript = document.createElement("script");
        newScript.setAttribute("data-recipe-modal", "true");

        if (script.src) {
          newScript.src = script.src;
        } else {
          newScript.textContent = `
            (function() {
                ${script.textContent}
            })();
        `;
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
          if (typeof clickedCard !== "undefined") {
            clickedCard.classList.remove("loading");
          }
        });

        modalElement.addEventListener("shown.bs.modal", function () {
          if (typeof clickedCard !== "undefined") {
            clickedCard.classList.remove("loading");
          }
        });
      } else {
        if (typeof clickedCard !== "undefined") {
          clickedCard.classList.remove("loading");
        }
      }
    })
    .catch((err) => {
      console.error("Failed to fetch recipe details", err);
      showError("Failed to load recipe details. Please try again.");
      if (typeof clickedCard !== "undefined") {
        clickedCard.classList.remove("loading");
      }
    });
}

function removeRecipeCard(recipeId) {
  const recipeCard = document.querySelector(`[data-recipe-id="${recipeId}"]`);
  if (recipeCard) {
    recipeCard.classList.add("removing");
    setTimeout(() => {
      recipeCard.remove();
      updateDisplayCount();
      updateBulkActionButtons();
      updateSelectAllState();

      if (document.querySelectorAll(".recipe-card").length === 0) {
        location.reload();
      }
    }, 500);
  }
}

function updateDisplayCount(count = null) {
  const displayCountElement = document.getElementById("displayCount");
  if (count === null) {
    count = document.querySelectorAll(
      '.recipe-card:not([style*="display: none"])'
    ).length;
  }
  displayCountElement.textContent = count;
}

function showLoadingOverlay() {
  document.getElementById("loadingOverlay").style.display = "flex";
}

function hideLoadingOverlay() {
  document.getElementById("loadingOverlay").style.display = "none";
}

function createToast(message, type = "info") {
  const toastContainer =
    document.getElementById("toast-container") || createToastContainer();

  const toastId = "toast-" + Date.now();
  const toast = document.createElement("div");
  toast.id = toastId;
  toast.className = `toast align-items-center text-white bg-${type} border-0`;
  toast.setAttribute("role", "alert");
  toast.setAttribute("aria-live", "assertive");
  toast.setAttribute("aria-atomic", "true");

  const iconMap = {
    success: "fas fa-check-circle",
    danger: "fas fa-exclamation-circle",
    warning: "fas fa-exclamation-triangle",
    info: "fas fa-info-circle",
  };

  toast.innerHTML = `
    <div class="d-flex">
        <div class="toast-body">
            <i class="${iconMap[type] || iconMap.info} me-2"></i>
            ${message}
        </div>
        <button type="button" class="btn-close btn-close-white me-2 m-auto" onclick="removeToast('${toastId}')"></button>
    </div>
`;

  toastContainer.appendChild(toast);

  toast.style.display = "block";
  setTimeout(() => {
    toast.classList.add("show");
  }, 100);

  setTimeout(() => {
    removeToast(toastId);
  }, 5000);
}

function removeToast(toastId) {
  const toast = document.getElementById(toastId);
  if (toast) {
    toast.classList.remove("show");
    setTimeout(() => {
      if (toast.parentNode) {
        toast.remove();
      }
    }, 300);
  }
}

function createToastContainer() {
  const container = document.createElement("div");
  container.id = "toast-container";
  container.className = "toast-container position-fixed top-0 end-0 p-3";
  container.style.zIndex = "10000";
  document.body.appendChild(container);
  return container;
}

function showSuccess(message) {
  createToast(message, "success");
}

function showError(message) {
  createToast(message, "danger");
}

function showWarning(message) {
  createToast(message, "warning");
}

function showInfo(message) {
  createToast(message, "info");
}

function refreshPendingRecipes() {
  showLoadingOverlay();
  location.reload();
}

document.addEventListener("keydown", function (e) {
  if (e.key === "Escape") {
    closeModal();
  }
});

console.log("Admin Panel initialized successfully");

function hideRecipeDetails() {
  const modalWindow = document.getElementById("modalWindow");
  const recipeDetailsModal = modalWindow?.querySelector(".modal");
  if (recipeDetailsModal) {
    const modalInstance = bootstrap.Modal.getInstance(recipeDetailsModal);
    if (modalInstance) {
      modalInstance.hide();
    }
  }
}

let currentRecipeId = null;

window.viewIngredientReview = async function (ingredientId) {
  console.log("Global viewIngredientReview called with ID:", ingredientId);

  const recipeModal = document.getElementById("recipeModal");
  if (recipeModal) {
    const recipeModalInstance = bootstrap.Modal.getInstance(recipeModal);
    if (recipeModalInstance) {
      recipeModalInstance.hide();
    }
  }

  setTimeout(async () => {
    const modal = document.getElementById("ingredientReviewModal");
    const content = document.getElementById("ingredientReviewContent");

    if (!modal || !content) {
      console.error("Modal or content container not found");
      alert("Modal components not found");
      return;
    }

    try {
      content.innerHTML = `
        <div class="text-center p-4">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
            <p class="mt-2">Loading ingredient details...</p>
        </div>
    `;

      const bootstrapModal = new bootstrap.Modal(modal);
      bootstrapModal.show();

      const response = await fetch(
        `/Admin/GetIngredientReview/${ingredientId}`,
        {
          method: "GET",
          headers: {
            "Content-Type": "application/json",
            "X-Requested-With": "XMLHttpRequest",
          },
        }
      );

      if (response.ok) {
        const html = await response.text();
        content.innerHTML = html;
      } else {
        content.innerHTML = `
            <div class="alert alert-danger">
                <h6>Error ${response.status}</h6>
                <p>Failed to load ingredient details.</p>
            </div>
        `;
      }
    } catch (error) {
      console.error("Error:", error);
      content.innerHTML = `
        <div class="alert alert-danger">
            <h6>Connection Error</h6>
            <p>${error.message}</p>
        </div>
    `;
    }
  }, 300);
};

function showRecipeModal() {
  if (currentRecipeId) {
    showRecipeDetails(currentRecipeId, true, "Buttons");
  } else {
    console.error("No recipe ID stored - cannot restore recipe modal");
  }
}
