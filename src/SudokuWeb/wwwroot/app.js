// Front-end logic for the Sudoku page.
// Talks to the ASP.NET API to get a new puzzle and to check the player's answer.
(() => {
    "use strict";

    const SIZE = 9;

    // ---------- References to page elements ----------
    const boardEl = document.getElementById("board");
    const difficultyEl = document.getElementById("difficulty");
    const newGameBtn = document.getElementById("new-game");
    const checkBtn = document.getElementById("check");
    const clearBtn = document.getElementById("clear");
    const messageEl = document.getElementById("message");

    // Id of the current game on the server (the solution lives there, not in the browser).
    let gameId = null;

    // Counts puzzle requests. If the difficulty is changed several times quickly,
    // only the newest request's board is drawn; older answers are ignored.
    let latestRequest = 0;

    // ---------- Helpers ----------

    /** Shows a message under the board. kind = "success" | "error" | "info". */
    function showMessage(text, kind = "info") {
        messageEl.textContent = text;
        messageEl.className = `message ${kind}`;
    }

    /** Returns the <td> for a given row/column. */
    function getCell(row, col) {
        return boardEl.rows[row].cells[col];
    }

    /** Removes the red/green highlighting left over from a previous check. */
    function clearHighlights() {
        boardEl.querySelectorAll("td").forEach(td => td.classList.remove("incorrect", "solved"));
    }

    /** Builds a dropdown with a blank option plus the numbers 1-9. */
    function createDropdown(row, col) {
        const select = document.createElement("select");
        select.setAttribute("aria-label", `Row ${row + 1}, column ${col + 1}`);

        // Blank option (value 0) means the cell is still empty.
        select.add(new Option("", "0"));
        for (let n = 1; n <= SIZE; n++) {
            select.add(new Option(String(n), String(n)));
        }

        // Changing a value removes the old check highlight for that cell.
        select.addEventListener("change", () => {
            select.parentElement.classList.remove("incorrect");
            boardEl.querySelectorAll("td.solved").forEach(td => td.classList.remove("solved"));
        });

        return select;
    }

    // ---------- Drawing the board ----------

    /**
     * Draws the 9x9 grid. Non-zero numbers are the fixed starting clues;
     * zeros become dropdowns the player can pick 1-9 from.
     */
    function renderBoard(puzzle) {
        boardEl.innerHTML = "";

        for (let row = 0; row < SIZE; row++) {
            const tr = boardEl.insertRow();
            for (let col = 0; col < SIZE; col++) {
                const td = tr.insertCell();
                const value = puzzle[row][col];

                if (value !== 0) {
                    // Starting clue: shown as plain, non-editable text.
                    td.textContent = value;
                    td.classList.add("given");
                } else {
                    // Empty cell: the player chooses a number from the dropdown.
                    td.appendChild(createDropdown(row, col));
                }
            }
        }
    }

    /**
     * Reads the current board from the page into a 9x9 array of numbers
     * (0 = the player hasn't chosen a number yet).
     */
    function readBoard() {
        const board = [];
        for (let row = 0; row < SIZE; row++) {
            const values = [];
            for (let col = 0; col < SIZE; col++) {
                const td = getCell(row, col);
                const select = td.querySelector("select");
                values.push(select ? Number(select.value) : Number(td.textContent));
            }
            board.push(values);
        }
        return board;
    }

    // ---------- Talking to the server ----------

    /**
     * Requests a new random puzzle for the selected difficulty and draws it.
     * Called on page load, by the "New Game" button, and whenever the difficulty changes.
     */
    async function newGame() {
        const requestId = ++latestRequest;
        const difficultyName = difficultyEl.options[difficultyEl.selectedIndex].text;

        newGameBtn.disabled = true;
        checkBtn.disabled = true;
        showMessage(`Generating a new ${difficultyName} puzzle...`);

        try {
            const difficulty = encodeURIComponent(difficultyEl.value);
            const response = await fetch(`/api/sudoku/new?difficulty=${difficulty}`);
            if (!response.ok) {
                throw new Error(`Server returned ${response.status}`);
            }

            const data = await response.json();
            if (requestId !== latestRequest) {
                return; // a newer request was started meanwhile; let that one draw the board
            }

            gameId = data.gameId;
            renderBoard(data.puzzle);
            showMessage(`New ${difficultyName} puzzle. Good luck!`);
        } catch (err) {
            if (requestId === latestRequest) {
                showMessage(`Could not load a puzzle: ${err.message}`, "error");
            }
        } finally {
            // Only the newest request re-enables the buttons.
            if (requestId === latestRequest) {
                newGameBtn.disabled = false;
                checkBtn.disabled = false;
            }
        }
    }

    /**
     * Sends the player's board to the server, which compares it with the
     * solution created alongside the puzzle, then highlights the result.
     */
    async function checkAnswer() {
        if (!gameId) {
            return;
        }

        clearHighlights();
        checkBtn.disabled = true;

        try {
            const response = await fetch(`/api/sudoku/${gameId}/check`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ board: readBoard() }),
            });

            const result = await response.json();
            if (!response.ok) {
                throw new Error(result.error || `Server returned ${response.status}`);
            }

            // Mark every wrong number in red.
            result.incorrectCells.forEach(([row, col]) => getCell(row, col).classList.add("incorrect"));

            // Pick the message based on the outcome.
            if (result.solved) {
                boardEl.querySelectorAll("td").forEach(td => td.classList.add("solved"));
                showMessage("Congratulations! You solved the puzzle!", "success");
            } else if (result.incorrectCells.length > 0) {
                const wrong = result.incorrectCells.length;
                showMessage(
                    `${wrong} incorrect ${wrong === 1 ? "cell" : "cells"} highlighted in red` +
                    (result.emptyCells > 0 ? `, and ${result.emptyCells} still empty.` : "."),
                    "error");
            } else {
                showMessage(`Everything so far is correct. ${result.emptyCells} cells left to fill.`, "info");
            }
        } catch (err) {
            showMessage(`Could not check the answer: ${err.message}`, "error");
        } finally {
            checkBtn.disabled = false;
        }
    }

    /** Resets every player dropdown back to blank (starting clues stay). */
    function clearEntries() {
        boardEl.querySelectorAll("select").forEach(select => { select.value = "0"; });
        clearHighlights();
        showMessage("Your entries were cleared.");
    }

    // ---------- Wire up buttons and load the first puzzle ----------
    newGameBtn.addEventListener("click", newGame);
    // Changing the difficulty immediately starts a fresh board at that difficulty.
    difficultyEl.addEventListener("change", newGame);
    checkBtn.addEventListener("click", checkAnswer);
    clearBtn.addEventListener("click", clearEntries);

    newGame();
})();
