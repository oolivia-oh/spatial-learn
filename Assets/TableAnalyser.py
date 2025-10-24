import csv
from collections import Counter

def count_frequencies(csv_file):
    frequencies = Counter()
    
    # Open CSV file
    with open(csv_file, mode='r', newline='', encoding='utf-8') as file:
        reader = csv.reader(file)
        titles = []
        table = {}
        
        # Count frequency of each cell's value
        for row in reader:
            if not titles:
                for value in row:
                    value = value.strip()  # remove extra spaces
                    titles.append(value)
                    table[value] = List()
            else:
                for i, value in enumerate(row):
                    value = value.strip()  # remove extra spaces
                    table[value].append()
                    
    return frequencies

def generate_lesson():
    questions
