# DME Workflow Parser

> Synapse Health's technical assessment project.

## Tasks Accomplished

> I believe I his all the required goals and all the optional goals except `replacing the manual extraction logic with an LLM`.
So that would be:
>  * [x] Refactor the logic into well-named, testable methods.
>  * [x] Introduce logging and basic error handling.
>  * [x] Write at least one unit test.
>  * [x] Replace misleading or unclear comments with helpful ones.
>  * [x] Keep it functional.
>  * [x] (Optional) - Accept multiple input formats (e.g., JSON-wrapped notes).
>  * [x] (Optional) - Add configurability for file path or API endpoint.
>  * [x] (Optional) - Support more DME device types or qualifiers.
>  * [ ] (Optional) - Replace the manual extraction logic with an LLM (e.g., OpenAI or Azure OpenAI).

## Project Submission Info

>  * I used `VS code` for my IDE.
>  * I used both `GitHub Copilot` (within vs code) and `MS Copilot` (in `MS Edge`) to aid me in this endeavor.
>  * I made a couple of assumptions when working on this project:
>    * In `JSON` note files, notes are store in the `data` property.
>    * Based on how the notes text is formatted:
>      1. Each piece of information is on its own line, starting with a label (e.g., "Patient Name:", "DOB:", "Diagnosis:", etc.).
>      2. The prescription (or recommendation) line however may contain multiple pieces of information in a single line.
>      3. The usage line seems to be a phrase that may contain a keyword or key-phrase for a single piece of information, so it warrants a "contains" search.
>      4. My approach was then to break down each line into key/value pairs and assign the values 1 to 1 with the Order properties, except for prescription.
>      5. There, I extracted multiple pieces of information by checking if certain keywords exist in the line.

## How To Run The Project

> Clone and open up the project in `VS code` as normal. Tests can be run via the IDE's test tab or navigating to the `Tests` folder in the terminal and running `dotnet test`. For debugging, press F5 then follow prompts to to setup the debugger for a `C#` project and the `App` startup project.  
** Important note: make sure to modify the paths in `appsettings.json` to match you desired folder structure. **