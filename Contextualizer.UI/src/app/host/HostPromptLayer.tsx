import { useEffect, useMemo, useRef, useState } from 'react';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '../components/ui/dialog';
import { Button } from '../components/ui/button';
import { Input } from '../components/ui/input';
import { Label } from '../components/ui/label';
import { Textarea } from '../components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../components/ui/select';
import { ScrollArea } from '../components/ui/scroll-area';
import { Checkbox } from '../components/ui/checkbox';
import { Alert, AlertDescription, AlertTitle } from '../components/ui/alert';
import { cn } from '../components/ui/utils';
import { addWebView2MessageListener, postWebView2Message } from './webview2Bridge';

type SelectionItemDto = { value: string; display: string };

type UserInputRequestDto = {
  key: string;
  title: string;
  message: string;
  validation_regex?: string;
  is_required?: boolean;
  is_selection_list?: boolean;
  is_password?: boolean;
  selection_items?: SelectionItemDto[];
  is_multi_select?: boolean;
  is_file_picker?: boolean;
  is_multi_line?: boolean;
  default_value?: string;
};

type ConfirmRequest = {
  type: 'ui_confirm_request';
  requestId: string;
  title?: string;
  message: string;
};

type UserInputRequestMsg = {
  type: 'ui_user_input_request';
  requestId: string;
  request: UserInputRequestDto;
  context?: Record<string, string>;
};

type UserInputNavRequestMsg = {
  type: 'ui_user_input_navigation_request';
  requestId: string;
  request: UserInputRequestDto;
  context: Record<string, string>;
  canGoBack: boolean;
  currentStep: number;
  totalSteps: number;
};

type FileDialogResponseMsg = {
  type: 'ui_open_file_dialog_response';
  requestId: string;
  cancelled?: boolean;
  path?: string;
  paths?: string[];
  error?: string;
};

type Prompt =
  | { kind: 'confirm'; msg: ConfirmRequest }
  | { kind: 'input'; msg: UserInputRequestMsg }
  | { kind: 'nav'; msg: UserInputNavRequestMsg };

function parsePrompt(message: unknown): Prompt | null {
  if (!message || typeof message !== 'object') return null;
  const m = message as Record<string, unknown>;
  const type = m.type;
  if (type === 'ui_confirm_request') return { kind: 'confirm', msg: message as ConfirmRequest };
  if (type === 'ui_user_input_request') return { kind: 'input', msg: message as UserInputRequestMsg };
  if (type === 'ui_user_input_navigation_request') return { kind: 'nav', msg: message as UserInputNavRequestMsg };
  return null;
}

function isFileDialogResponse(message: unknown): message is FileDialogResponseMsg {
  return !!message && typeof message === 'object' && (message as any).type === 'ui_open_file_dialog_response';
}

function defaultValueForRequest(req: UserInputRequestDto, context?: Record<string, string>): string {
  if (context && req.key && typeof context[req.key] === 'string' && context[req.key].length > 0) {
    return context[req.key];
  }
  return req.default_value ?? '';
}

export function HostPromptLayer() {
  const [queue, setQueue] = useState<Prompt[]>([]);
  const current = queue.length > 0 ? queue[0] : null;

  const [value, setValue] = useState('');
  const [selectedValues, setSelectedValues] = useState<string[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [pendingFileRequestId, setPendingFileRequestId] = useState<string | null>(null);
  const pendingFileRequestIdRef = useRef<string | null>(null);

  useEffect(() => {
    pendingFileRequestIdRef.current = pendingFileRequestId;
  }, [pendingFileRequestId]);

  // IMPORTANT: subscribe ONCE to WebView2 messages.
  // Re-subscribing (e.g. when pendingFileRequestId changes) can create tiny gaps where messages are dropped,
  // which is exactly what breaks multi-step user inputs.
  useEffect(() => {
    const unsubscribe = addWebView2MessageListener((payload) => {
      // Enqueue prompt requests
      const prompt = parsePrompt(payload);
      if (prompt) {
        const requestId = prompt.msg.requestId;
        if (!requestId) return;
        setQueue((prev) => {
          if (prev.some((p) => p.msg.requestId === requestId)) return prev;
          return [...prev, prompt];
        });
        return;
      }

      // Handle file dialog responses for prompt-local file picker
      const pendingId = pendingFileRequestIdRef.current;
      if (pendingId && isFileDialogResponse(payload) && payload.requestId === pendingId) {
        pendingFileRequestIdRef.current = null;
        setPendingFileRequestId(null);

        if ((payload as any).cancelled) return;

        if (Array.isArray(payload.paths) && payload.paths.length > 0) {
          setSelectedValues(payload.paths);
          setValue(payload.paths.join(','));
          return;
        }

        if (typeof payload.path === 'string') {
          setSelectedValues([]);
          setValue(payload.path);
        }
      }
    });

    return () => {
      try {
        unsubscribe();
      } catch {
        // ignore
      }
    };
  }, []);

  // Initialize local form state when the current prompt changes
  useEffect(() => {
    setError(null);
    setPendingFileRequestId(null);

    if (!current) {
      setValue('');
      setSelectedValues([]);
      return;
    }

    if (current.kind === 'input') {
      const req = current.msg.request;
      const initial = defaultValueForRequest(req, current.msg.context);

      if (req.is_selection_list && req.is_multi_select) {
        const list = initial
          .split(',')
          .map((s) => s.trim())
          .filter(Boolean);
        setSelectedValues(list);
        setValue(list.join(','));
      } else {
        setSelectedValues([]);
        setValue(initial);
      }
      return;
    }

    if (current.kind === 'nav') {
      const req = current.msg.request;
      const initial = defaultValueForRequest(req, current.msg.context);

      if (req.is_selection_list && req.is_multi_select) {
        const list = initial
          .split(',')
          .map((s) => s.trim())
          .filter(Boolean);
        setSelectedValues(list);
        setValue(list.join(','));
      } else {
        setSelectedValues([]);
        setValue(initial);
      }
    }
  }, [current?.msg.requestId]);

  const title = useMemo(() => {
    if (!current) return '';
    if (current.kind === 'confirm') return current.msg.title ?? 'Confirmation';
    return current.msg.request?.title ?? 'Input Required';
  }, [current]);

  const description = useMemo(() => {
    if (!current) return '';
    if (current.kind === 'confirm') return current.msg.message ?? '';
    return current.msg.request?.message ?? '';
  }, [current]);

  const closeAsCancel = () => {
    if (!current) return;

    if (current.kind === 'confirm') {
      postWebView2Message({
        type: 'ui_confirm_response',
        requestId: current.msg.requestId,
        confirmed: false,
      });
    } else if (current.kind === 'input') {
      postWebView2Message({
        type: 'ui_user_input_response',
        requestId: current.msg.requestId,
        cancelled: true,
      });
    } else {
      postWebView2Message({
        type: 'ui_user_input_navigation_response',
        requestId: current.msg.requestId,
        action: 'cancel',
      });
    }

    setQueue((prev) => prev.slice(1));
  };

  const validate = (req: UserInputRequestDto): boolean => {
    setError(null);

    const required = req.is_required !== false;

    if (req.is_selection_list && req.is_multi_select) {
      if (required && selectedValues.length === 0) {
        setError('Please select at least one item.');
        return false;
      }
      return true;
    }

    if (required && value.trim().length === 0) {
      setError('Input is required. Please enter a value.');
      return false;
    }

    const pattern = req.validation_regex;
    if (pattern && value.trim().length > 0) {
      try {
        const re = new RegExp(pattern);
        if (!re.test(value)) {
          setError('Invalid input format. Please follow the expected format.');
          return false;
        }
      } catch {
        // If the host provided a regex that JS can't compile, don't block the user.
      }
    }

    return true;
  };

  const submitInput = () => {
    if (!current || current.kind === 'confirm') return;
    const req = current.msg.request;
    if (!validate(req)) return;

    if (current.kind === 'input') {
      postWebView2Message({
        type: 'ui_user_input_response',
        requestId: current.msg.requestId,
        cancelled: false,
        value: req.is_selection_list && req.is_multi_select ? selectedValues.join(',') : value,
        selectedValues: req.is_selection_list && req.is_multi_select ? selectedValues : undefined,
      });
      setQueue((prev) => prev.slice(1));
      return;
    }

    // navigation
    postWebView2Message({
      type: 'ui_user_input_navigation_response',
      requestId: current.msg.requestId,
      action: 'next',
      value: req.is_selection_list && req.is_multi_select ? selectedValues.join(',') : value,
      selectedValues: req.is_selection_list && req.is_multi_select ? selectedValues : undefined,
    });
    setQueue((prev) => prev.slice(1));
  };

  const goBack = () => {
    if (!current || current.kind !== 'nav') return;
    postWebView2Message({
      type: 'ui_user_input_navigation_response',
      requestId: current.msg.requestId,
      action: 'back',
    });
    setQueue((prev) => prev.slice(1));
  };

  const confirmYes = () => {
    if (!current || current.kind !== 'confirm') return;
    postWebView2Message({
      type: 'ui_confirm_response',
      requestId: current.msg.requestId,
      confirmed: true,
    });
    setQueue((prev) => prev.slice(1));
  };

  const browseFile = () => {
    if (!current || (current.kind !== 'input' && current.kind !== 'nav')) return;
    const req = current.msg.request;
    if (!req.is_file_picker) return;

    const requestId = `${Date.now()}_${Math.random()}`;
    setPendingFileRequestId(requestId);

    postWebView2Message({
      type: 'ui_open_file_dialog_request',
      requestId,
      title: req.title || 'Select File',
      filter: 'All Files|*.*',
      multiSelect: !!req.is_multi_select,
    });
  };

  const renderInput = () => {
    if (!current || current.kind === 'confirm') return null;
    const req = current.msg.request;
    const required = req.is_required !== false;

    if (req.is_file_picker) {
      return (
        <div className="space-y-2">
          <Label className="text-sm font-semibold">
            {req.title}
            {required && <span className="text-destructive"> *</span>}
          </Label>
          <div className="flex gap-2">
            <Input value={value} readOnly placeholder="Select a file..." className="flex-1" />
            <Button variant="outline" onClick={browseFile} disabled={!!pendingFileRequestId}>
              Browse
            </Button>
          </div>
          <p className="text-xs text-muted-foreground">File path will be returned to the host.</p>
        </div>
      );
    }

    if (req.is_selection_list) {
      const items = req.selection_items ?? [];

      if (req.is_multi_select) {
        return (
          <div className="space-y-2">
            <Label className="text-sm font-semibold">
              {req.title}
              {required && <span className="text-destructive"> *</span>}
            </Label>
            <ScrollArea className="h-56 border rounded-md bg-card">
              <div className="p-3 space-y-2">
                {items.length === 0 ? (
                  <div className="text-sm text-muted-foreground">No options provided.</div>
                ) : (
                  items.map((it) => {
                    const checked = selectedValues.includes(it.value);
                    return (
                      <label
                        key={it.value}
                        className={cn(
                          'flex items-center gap-3 p-2 rounded-md border bg-background hover:bg-accent/40 transition-colors cursor-pointer',
                        )}
                      >
                        <Checkbox
                          checked={checked}
                          onCheckedChange={(next) => {
                            const isChecked = next === true;
                            setSelectedValues((prev) => {
                              const set = new Set(prev);
                              if (isChecked) set.add(it.value);
                              else set.delete(it.value);
                              const list = Array.from(set);
                              setValue(list.join(','));
                              return list;
                            });
                          }}
                        />
                        <div className="min-w-0">
                          <div className="text-sm font-medium truncate">{it.display || it.value}</div>
                          {it.display && it.display !== it.value && (
                            <div className="text-xs text-muted-foreground truncate">{it.value}</div>
                          )}
                        </div>
                      </label>
                    );
                  })
                )}
              </div>
            </ScrollArea>
            <p className="text-xs text-muted-foreground">{selectedValues.length} selected</p>
          </div>
        );
      }

      return (
        <div className="space-y-2">
          <Label className="text-sm font-semibold">
            {req.title}
            {required && <span className="text-destructive"> *</span>}
          </Label>
          <Select value={value} onValueChange={setValue}>
            <SelectTrigger>
              <SelectValue placeholder="Select an option" />
            </SelectTrigger>
            <SelectContent>
              {items.length === 0 ? (
                <SelectItem value="__none__" disabled>
                  No options
                </SelectItem>
              ) : (
                items.map((it) => (
                  <SelectItem key={it.value} value={it.value}>
                    {it.display || it.value}
                  </SelectItem>
                ))
              )}
            </SelectContent>
          </Select>
        </div>
      );
    }

    if (req.is_password) {
      return (
        <div className="space-y-2">
          <Label className="text-sm font-semibold">
            {req.title}
            {required && <span className="text-destructive"> *</span>}
          </Label>
          <Input type="password" value={value} onChange={(e) => setValue(e.target.value)} />
        </div>
      );
    }

    if (req.is_multi_line) {
      return (
        <div className="space-y-2">
          <Label className="text-sm font-semibold">
            {req.title}
            {required && <span className="text-destructive"> *</span>}
          </Label>
          <Textarea value={value} onChange={(e) => setValue(e.target.value)} className="min-h-28" />
        </div>
      );
    }

    return (
      <div className="space-y-2">
        <Label className="text-sm font-semibold">
          {req.title}
          {required && <span className="text-destructive"> *</span>}
        </Label>
        <Input value={value} onChange={(e) => setValue(e.target.value)} />
      </div>
    );
  };

  return (
    <Dialog open={!!current} onOpenChange={(open) => !open && closeAsCancel()}>
      <DialogContent key={current?.msg.requestId ?? 'none'}>
        <DialogHeader>
          <DialogTitle className="text-lg">{title}</DialogTitle>
          <DialogDescription className="whitespace-pre-wrap">{description}</DialogDescription>
        </DialogHeader>

        {current?.kind === 'nav' && (
          <div className="text-xs text-muted-foreground">
            Step {current.msg.currentStep + 1} of {current.msg.totalSteps}
          </div>
        )}

        {error && (
          <Alert variant="destructive">
            <AlertTitle>Validation error</AlertTitle>
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}

        {current && current.kind !== 'confirm' && renderInput()}

        <DialogFooter>
          {current?.kind === 'confirm' ? (
            <>
              <Button variant="outline" onClick={closeAsCancel}>
                Cancel
              </Button>
              <Button onClick={confirmYes}>Confirm</Button>
            </>
          ) : current?.kind === 'nav' ? (
            <>
              <Button variant="outline" onClick={closeAsCancel}>
                Cancel
              </Button>
              <Button variant="outline" onClick={goBack} disabled={!current.msg.canGoBack}>
                Back
              </Button>
              <Button onClick={submitInput}>Next</Button>
            </>
          ) : (
            <>
              <Button variant="outline" onClick={closeAsCancel}>
                Cancel
              </Button>
              <Button onClick={submitInput}>OK</Button>
            </>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}


