import tkinter as tk
from tkinter import filedialog, messagebox
import re
import os

def extract_mods_block(binary_data):
    pattern = re.compile(rb"<UsedMods>.*?</UsedMods>", re.DOTALL)
    return pattern.search(binary_data)

def clear_modlist(target_path, output_path):
    with open(target_path, "rb") as f:
        target_bin = f.read()

    target_match = extract_mods_block(target_bin)

    if not target_match:
        messagebox.showerror("Error", "<UsedMods> section not found in the target file.")
        return

    empty_modlist = b"<UsedMods></UsedMods>"
    original_block = target_match.group(0)
    original_len = len(original_block)
    padded_replacement = empty_modlist + b" " * (original_len - len(empty_modlist))

    start, end = target_match.span()
    updated_bin = target_bin[:start] + padded_replacement + target_bin[end:]

    with open(output_path, "wb") as f:
        f.write(updated_bin)

    messagebox.showinfo("Success", f"Modlist cleared successfully!\nSaved to:\n{output_path}")

def browse_file(entry_field, update_output=False):
    file_path = filedialog.askopenfilename(filetypes=[("WarHorse Save Files", "*.whs")])
    if file_path:
        entry_field.delete(0, tk.END)
        entry_field.insert(0, file_path)

        if update_output:
            output_dir = os.path.dirname(file_path)
            filename = os.path.basename(file_path)
            auto_output_path = os.path.join(output_dir, filename)
            output_entry.delete(0, tk.END)
            output_entry.insert(0, auto_output_path)

def browse_output_file(entry_field):
    file_path = filedialog.asksaveasfilename(defaultextension=".whs", filetypes=[("WarHorse Save Files", "*.whs")])
    if file_path:
        entry_field.delete(0, tk.END)
        entry_field.insert(0, file_path)

def run_tool():
    target = target_entry.get()
    output = output_entry.get()

    if not all([target, output]):
        messagebox.showerror("Error", "Please fill in all file paths.")
        return

    if not os.path.exists(target):
        messagebox.showerror("Error", "Target file does not exist.")
        return

    clear_modlist(target, output)

def show_help():
    instructions = (
        "To use:\n\n"
        "1. Put the broken save into [TARGET SAVE]\n\n"
        "2. Output will automatically be named the same as the Target save, but can be changed manually\n\n"
        "3. Click 'Run Tool'"
    )
    messagebox.showinfo("Help - Instructions", instructions)

# GUI Setup
root = tk.Tk()
root.title("Kingdom Come Modlist Remover Tool")
root.geometry("750x300")
root.minsize(600, 250)
root.resizable(True, True)

# Menu bar
menu_bar = tk.Menu(root)
help_menu = tk.Menu(menu_bar, tearoff=0)
help_menu.add_command(label="How to Use", command=show_help)
menu_bar.add_cascade(label="Help", menu=help_menu)
root.config(menu=menu_bar)

# Frame with grid layout
frame = tk.Frame(root, padx=10, pady=10)
frame.pack(fill="both", expand=True)

frame.grid_columnconfigure(0, weight=2)
frame.grid_columnconfigure(1, weight=5)
frame.grid_columnconfigure(2, weight=1)

def create_row(row, label_text, entry_var, browse_command):
    label = tk.Label(frame, text=label_text, anchor='w')
    label.grid(row=row, column=0, sticky="nsew", padx=5, pady=5)

    entry = tk.Entry(frame, textvariable=entry_var)
    entry.grid(row=row, column=1, sticky="nsew", padx=5, pady=5)

    button = tk.Button(frame, text="Browse", command=browse_command)
    button.grid(row=row, column=2, sticky="nsew", padx=5, pady=5)

    return entry

target_var = tk.StringVar()
output_var = tk.StringVar()

target_entry = create_row(0, "Target Save (<UsedMods> will be cleared):", target_var, lambda: browse_file(target_entry, update_output=True))
output_entry = create_row(1, "Output Save (Default set to same location as Target save):", output_var, lambda: browse_output_file(output_entry))

# Run Button
run_btn = tk.Button(root, text="Run Tool", command=run_tool,
                    bg="#610000", fg="white", font=("Arial", 15, "bold"),
                    padx=20, pady=10)
run_btn.pack(pady=20)

root.mainloop()
