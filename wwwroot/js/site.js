// Helper utilities for custom search, autocomplete dropdown, date/time picker, and page animations.

function debounce(callback, wait) {
    let timeout = null;
    return function () {
        const context = this;
        const args = arguments;
        clearTimeout(timeout);
        timeout = setTimeout(function () {
            callback.apply(context, args);
        }, wait);
    };
}

function formatDateTime(value, culture) {
    if (!value) return "";
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return value;
    if (culture === "hr") {
        return `${String(date.getDate()).padStart(2, "0")}.${String(date.getMonth() + 1).padStart(2, "0")}.${date.getFullYear()} ${String(date.getHours()).padStart(2, "0")}:${String(date.getMinutes()).padStart(2, "0")}`;
    }
    return `${String(date.getMonth() + 1).padStart(2, "0")}/${String(date.getDate()).padStart(2, "0")}/${date.getFullYear()} ${String(date.getHours()).padStart(2, "0")}:${String(date.getMinutes()).padStart(2, "0")}`;
}

function parseDateTime(text, culture) {
    if (!text) return null;
    const parts = text.trim().split(" ");
    if (parts.length < 2) return null;
    const datePart = parts[0];
    const timePart = parts[1];
    const timeParts = timePart.split(":");
    if (timeParts.length !== 2) return null;
    const hours = parseInt(timeParts[0], 10);
    const minutes = parseInt(timeParts[1], 10);
    if (Number.isNaN(hours) || Number.isNaN(minutes)) return null;
    let year, month, day;
    if (culture === "hr") {
        const dateParts = datePart.split(".");
        if (dateParts.length !== 3) return null;
        day = parseInt(dateParts[0], 10);
        month = parseInt(dateParts[1], 10) - 1;
        year = parseInt(dateParts[2], 10);
    } else {
        const dateParts = datePart.split("/");
        if (dateParts.length !== 3) return null;
        month = parseInt(dateParts[0], 10) - 1;
        day = parseInt(dateParts[1], 10);
        year = parseInt(dateParts[2], 10);
    }
    const candidate = new Date(year, month, day, hours, minutes);
    return Number.isNaN(candidate.getTime()) ? null : candidate;
}

function initEntitySearch() {
    $(document).on("input", ".js-search-input", debounce(function () {
        const $search = $(this);
        const query = $search.val();
        const url = $search.data("search-url");
        const target = $search.data("target");
        if (!url || !target) return;
        $.get(url, { q: query }, function (html) {
            $(target).html(html).addClass("search-updated");
            window.setTimeout(function () {
                $(target).removeClass("search-updated");
            }, 400);
        });
    }, 250));
}

function initAutocompleteDropdowns() {
    $(document).on("input", ".autocomplete-input", debounce(function () {
        const $input = $(this);
        const searchUrl = $input.closest(".autocomplete-control").data("search-url");
        const query = $input.val();
        const $results = $input.closest(".autocomplete-control").find(".autocomplete-results");
        const hiddenInputId = $input.data("hidden-input");
        if (!searchUrl) return;
        if (!query || query.length < 1) {
            $results.addClass("d-none").empty();
            $(`#${hiddenInputId}`).val("");
            return;
        }
        $.get(searchUrl, { q: query }, function (items) {
            if (!Array.isArray(items) || items.length === 0) {
                $results.html('<div class="list-group-item disabled">No matches</div>').removeClass("d-none");
                return;
            }
            const rows = items.map(function (item) {
                return `<button type="button" class="list-group-item list-group-item-action autocomplete-suggestion" data-id="${item.id}" data-name="${item.text}">${item.text}</button>`;
            });
            $results.html(rows.join("")).removeClass("d-none");
        });
    }, 200));

    $(document).on("click", ".autocomplete-suggestion", function () {
        const $item = $(this);
        const $control = $item.closest(".autocomplete-control");
        const selectedId = $item.data("id");
        const selectedText = $item.data("name");
        const hiddenInputId = $control.find(".autocomplete-input").data("hidden-input");
        $control.find(".autocomplete-input").val(selectedText);
        $(`#${hiddenInputId}`).val(selectedId);
        $control.find(".autocomplete-results").addClass("d-none");
    });

    $(document).on("click", ".btn-clear-autocomplete", function () {
        const hiddenInputId = $(this).data("hidden-input");
        const $control = $(this).closest(".autocomplete-control");
        $control.find(".autocomplete-input").val("");
        $(`#${hiddenInputId}`).val("");
        $control.find(".autocomplete-results").addClass("d-none");
    });

    $(document).on("click", function (event) {
        if ($(event.target).closest(".autocomplete-control").length === 0) {
            $(".autocomplete-results").addClass("d-none");
        }
    });
}

function initDateTimePickers() {
    $(document).on("focus", ".js-datetime-picker", function () {
        const $input = $(this);
        const $container = $input.closest(".datetime-picker-field");
        const $panel = $container.find(".datetime-panel");
        const culture = $input.data("culture") || "hr";
        const current = parseDateTime($input.val(), culture) || new Date();
        $panel.empty();

        const monthLabels = culture === "hr" ? ["Sij", "Velj", "Ožu", "Tra", "Svi", "Lip", "Sri", "Kol", "Ruj", "Lis", "Stu", "Pro"] : ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
        const monthOptions = monthLabels.map((label, index) => `<option value="${index}" ${index === current.getMonth() ? "selected" : ""}>${label}</option>`).join("");

        const yearOptions = Array.from({ length: 5 }, (_, idx) => current.getFullYear() + idx).map(year => `<option value="${year}" ${year === current.getFullYear() ? "selected" : ""}>${year}</option>`).join("");
        const hourOptions = Array.from({ length: 24 }, (_, idx) => `<option value="${idx}" ${idx === current.getHours() ? "selected" : ""}>${String(idx).padStart(2, "0")}</option>`).join("");
        const minuteOptions = [0, 15, 30, 45].map(value => `<option value="${value}" ${value === current.getMinutes() ? "selected" : ""}>${String(value).padStart(2, "0")}</option>`).join("");

        const template = `
            <div class="datetime-panel-row mb-2">
                <div class="row gx-2">
                    <div class="col-4"><label class="form-label small mb-1">Day</label><input type="number" class="form-control form-control-sm js-dt-day" min="1" max="31" value="${current.getDate()}" /></div>
                    <div class="col-4"><label class="form-label small mb-1">Month</label><select class="form-select form-select-sm js-dt-month">${monthOptions}</select></div>
                    <div class="col-4"><label class="form-label small mb-1">Year</label><select class="form-select form-select-sm js-dt-year">${yearOptions}</select></div>
                </div>
            </div>
            <div class="datetime-panel-row mb-3">
                <div class="row gx-2">
                    <div class="col-6"><label class="form-label small mb-1">Hour</label><select class="form-select form-select-sm js-dt-hour">${hourOptions}</select></div>
                    <div class="col-6"><label class="form-label small mb-1">Minute</label><select class="form-select form-select-sm js-dt-minute">${minuteOptions}</select></div>
                </div>
            </div>
            <div class="d-flex gap-2">
                <button type="button" class="btn btn-primary btn-sm js-dt-set">Set</button>
                <button type="button" class="btn btn-outline-secondary btn-sm js-dt-now">Now</button>
            </div>
        `;

        $panel.append(template);
        $panel.removeClass("d-none");
    });

    $(document).on("click", ".js-dt-now", function () {
        const $panel = $(this).closest(".datetime-panel");
        const now = new Date();
        $panel.find(".js-dt-day").val(now.getDate());
        $panel.find(".js-dt-month").val(now.getMonth());
        $panel.find(".js-dt-year").val(now.getFullYear());
        $panel.find(".js-dt-hour").val(now.getHours());
        $panel.find(".js-dt-minute").val(Math.round(now.getMinutes() / 15) * 15 % 60);
    });

    $(document).on("click", ".js-dt-set", function () {
        const $panel = $(this).closest(".datetime-panel");
        const $container = $panel.closest(".datetime-picker-field");
        const $input = $container.find(".js-datetime-picker");
        const culture = $input.data("culture") || "hr";
        const year = parseInt($panel.find(".js-dt-year").val(), 10);
        const month = parseInt($panel.find(".js-dt-month").val(), 10);
        const day = parseInt($panel.find(".js-dt-day").val(), 10);
        const hour = parseInt($panel.find(".js-dt-hour").val(), 10);
        const minute = parseInt($panel.find(".js-dt-minute").val(), 10);
        const date = new Date(year, month, day, hour, minute);
        if (Number.isNaN(date.getTime())) {
            $input.addClass("is-invalid");
            return;
        }
        $input.removeClass("is-invalid");
        $input.val(formatDateTime(date, culture));
        $panel.addClass("d-none");
    });

    $(document).on("click", function (event) {
        if ($(event.target).closest(".datetime-picker-field").length === 0) {
            $(".datetime-panel").addClass("d-none");
        }
    });
}

function initCardAnimations() {
    $(document).on("mouseenter", ".metric-card", function () {
        $(this).addClass("animate-card");
    });
    $(document).on("mouseleave", ".metric-card", function () {
        $(this).removeClass("animate-card");
    });
}

function initDeleteConfirm() {
    $(document).on('click', '.btn-confirm-delete', function (e) {
        var $btn = $(this);
        var entity = $btn.data('entity') || 'item';
        var href = $btn.attr('href');
        if (!href) return;
        e.preventDefault();
        if (confirm('Delete this ' + entity + '? This will open the confirmation page.')) {
            window.location = href;
        }
    });
}

function initSearchableSelects() {
    $('.js-select-filter').each(function () {
        var $filter = $(this);
        var target = $filter.data('target-select');
        var $select = $(target);
        if ($select.length === 0) return;
        // store full options HTML
        $select.data('fullOptions', $select.html());
    });

    $(document).on('input', '.js-select-filter', debounce(function () {
        var $filter = $(this);
        var q = ($filter.val() || '').toLowerCase();
        var target = $filter.data('target-select');
        var $select = $(target);
        if ($select.length === 0) return;
        var full = $select.data('fullOptions') || '';
        if (!q) {
            $select.html(full);
            return;
        }
        var $temp = $('<select>' + full + '</select>');
        var matched = [];
        $temp.find('option').each(function () {
            var $o = $(this);
            if ($o.text().toLowerCase().indexOf(q) !== -1) matched.push($o.prop('outerHTML'));
        });
        if (matched.length === 0) {
            $select.html('<option disabled>No matches</option>');
        } else {
            $select.html(matched.join(''));
        }
    }, 150));
}

$(function () {
    initEntitySearch();
    initAutocompleteDropdowns();
    initDateTimePickers();
    initCardAnimations();
    initDeleteConfirm();
    initSearchableSelects();
});
