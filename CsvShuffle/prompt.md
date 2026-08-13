This is a freshly deployed MudBlazor WASM PWA template app. The purpose of this app is to accept data in the form of CSV
files and obfuscate certain columns of data (excluding the header row). This data may be dirty or bad. The app should
preserve the shape of the input data always. The data being obfuscated is PII data, but could be any data deemed
sensitive at the time of loading.

This should not look like an "AI Slop Application" with a lot of unnecessary text and graphics. 
It should be a clean, simple, and easy to use application that is fast and efficient as it is used as an internal tool.

The explicit PII data:

- First Name
- Middle Name
- Last Name
- Date
- Social Security Number
- Address
- Phone Number

Shape preservation rules:

- Name: alpha characters should be replaced. Vowels should be replaced with other vowels, Consonants should be replaced
  with other consonants. Upper case and Lower case characters should maintain their relative index in the string.
  Whitespace, accented characters, punctuation, etc. should all be preserved as is
- Date: Automatically parse date or date-time input, then obfuscate it by randomly varying the day +/- 10d,
  month +/- 2m, year +/- 5y. The augmented date should remain valid (a real date) after obfuscation. We should
  assume input date is also valid.
- Social Security Number: For clean input data, Social security number should randomize each digit in the field. For
  dirty, data (missing, bad format, etc.) it should preserve the data shape at input while randomizing only numeric
  digits. (all other characters present should remain as-is). Note: there should be a pre-check for duplicate SSN's in
  the input data.The output data should reflect this. If "Jane Doe, SSN 000-00-0001" appears in the data twice (two
  rows), then the SSN should match after obfuscation.
- Address: should follow the same principles. Replace all digits with random digits. Replace vowels with vowels,
  consonants with consonants, punctuation, accented, white space, and all other characters are preserved as-is.
- Phone number: follows the same rules/logic as SSN
- Generic: Should replace all alpha characters with random alpha characters, all digits with random digits, and preserve
  all other characters as-is. This is a catch-all for any other data that may be deemed sensitive at the time of
  loading.

The goal of this application:

- it should allow users to mark 0 to many columns in the table as:
    - Name (obfuscate using name rules)
    - SSN (obfuscate using ssn rules)
    - Date (obfuscate using date rules)
    - Phone (obfuscate using phone rules)
    - Address (obfuscate using address rules)
    - Generic (obfuscate using generic rules)
    - Clear (no obfuscation, as-is, input = output)
        - The default column selection should be Clear
- Use real data to create a safe mock-data-set to use for development testing where production data is forbidden. The
  mock data is sterilized of any real, usable, PII from actual people, but perfectly mirrors the shape of the prod data
  (source input errors and all).
- It should accept/read/open any valid CSV document
- it should show the input CSV data in a table ("excel like experience") where users can search and filter columns
- To be very light-weight and independent (self-contained exe • It will be running on macOS and Windows machines
  primarily Let me know if you have any follow-up questions before getting started on implementation
- The user should be able to apply the transformation via a button which will save the document at a user defined
  location appending _obfuscated to the original input file name.
- The application should be fast and handle large CSV files (100k+ rows) without crashing or slowing down the machine
- Must indicate progress to the user during the loading and obfuscation process visually
- Must support cancellation during the loading and obfuscation process
- Leverage MudBlazor components for the UI
- Allow filtering, sorting, and searching of the data in the table per column
- Should have a universal search filter function
- Column widths should be resizable by the user
- The column header (column name, column filter/sort/search, column width resize) should be sticky and remain visible
  while scrolling through the data.
- Must support pagination of the data in the table (page sizes: 100, 500, 1000)
- Allow users to Obfuscate the data via a button click (to generate the obfuscated view). 
  - Clicking "Obfuscate" multiple times should re-apply obfuscation on the original data
- Allow users to Save and Download the obfuscated data to a CSV file (with _obfuscated appended to the original file name)

Let me know if you have any follow up questions before getting started on implementation
