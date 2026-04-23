# Finder Agent Skills (Internal Design Reference)

## resolve_entity

### Purpose
Resolve user input into exactly one entity (party, account, or household).

---

### Responsibilities
- Call search_and_resolve
- Handle multiple candidates
- Drive disambiguation via user interaction
- Enforce single-entity resolution

---

### Behavior
1. Execute search
2. If no results → ask user to refine
3. If multiple results:
   - Present options
   - Ask user to select
   - Repeat until one entity is selected
4. Return resolved entity

---

### Output
{
  "entityId": "<id>",
  "entityType": "party | account | household",
  "displayName": "<name>"
}

---

### Guarantees
- Always returns exactly one entity
- Never returns ambiguous results
- Handles full user interaction loop

---

### Notes
- Stateful, multi-turn conceptual skill
- Not directly supported by VS Code, used for design clarity
