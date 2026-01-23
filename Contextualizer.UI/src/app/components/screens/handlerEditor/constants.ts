export const operatorOptions = [
  { value: 'and', label: 'AND (group)' },
  { value: 'or', label: 'OR (group)' },
  { value: 'equals', label: 'Equals' },
  { value: 'not_equals', label: 'Not Equals' },
  { value: 'greater_than', label: 'Greater Than' },
  { value: 'less_than', label: 'Less Than' },
  { value: 'contains', label: 'Contains' },
  { value: 'starts_with', label: 'Starts With' },
  { value: 'ends_with', label: 'Ends With' },
  { value: 'matches_regex', label: 'Matches Regex' },
  { value: 'is_empty', label: 'Is Empty' },
  { value: 'is_not_empty', label: 'Is Not Empty' },
];

export const httpMethods = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE'];
export const dbConnectors = ['mssql', 'plsql'];
