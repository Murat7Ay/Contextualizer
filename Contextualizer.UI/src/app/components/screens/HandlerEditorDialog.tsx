import React, { useState } from 'react';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../ui/tabs';
import { createHandler, deleteHandler, updateHandler } from '../../host/webview2Bridge';
import type { Props, EditorProps } from './handlerEditor/types';
import { getHandlerEditorTitle } from './handlerEditor/helpers';
import { PublishDialog } from './handlerEditor/publish';
import { useHandlerEditor } from './handlerEditor/useHandlerEditor';
import { HandlerEditorWizard } from './handlerEditor/HandlerEditorWizard';
import { HandlerEditorJson } from './handlerEditor/HandlerEditorJson';

export function HandlerEditorDialog({ open, mode, handlerName, onOpenChange, onSaved, onDeleted }: Props) {
  if (!open) return null;
  const title = getHandlerEditorTitle(mode, handlerName);
  return (
    <div className="max-w-5xl p-4 space-y-4">
      <div className="space-y-1">
        <div className="text-lg font-semibold">{title}</div>
        <p className="text-sm text-muted-foreground">
          Wizard for guided editing, or Advanced JSON for full control. Validation happens on save (host-side).
        </p>
      </div>
      <HandlerEditorBody
        open={open}
        mode={mode}
        handlerName={handlerName}
        onCancel={() => onOpenChange(false)}
        onSaved={onSaved}
        onDeleted={onDeleted}
      />
    </div>
  );
}

export function HandlerEditorBody({ open, mode, handlerName, onCancel, onSaved, onDeleted }: EditorProps) {
  const editor = useHandlerEditor(open, mode, handlerName, 'Regex', onSaved, onDeleted, onCancel);

  const saveFromWizard = () => {
    if (!editor.wizardValidation.ok) {
      editor.setError(editor.wizardValidation.errors.join('\n'));
      return;
    }
    editor.setLoading(true);
    if (mode === 'new') {
      const ok = createHandler(editor.draft, true);
      if (!ok) {
        editor.setLoading(false);
        editor.setError('Unable to send create request to host');
      }
    } else {
      const ok = updateHandler(handlerName ?? editor.draft.name, editor.draft, true);
      if (!ok) {
        editor.setLoading(false);
        editor.setError('Unable to send update request to host');
      }
    }
  };

  const saveFromJson = () => {
    try {
      const obj = JSON.parse(editor.jsonText);
      editor.setLoading(true);
      if (mode === 'new') {
        const ok = createHandler(obj, true);
        if (!ok) {
          editor.setLoading(false);
          editor.setError('Unable to send create request to host');
        }
      } else {
        const ok = updateHandler(handlerName ?? '', obj, true);
        if (!ok) {
          editor.setLoading(false);
          editor.setError('Unable to send update request to host');
        }
      }
    } catch (ex: any) {
      editor.setError(ex?.message ?? 'Invalid JSON');
    }
  };

  const onDelete = () => {
    if (!handlerName) return;
    editor.setLoading(true);
    const ok = deleteHandler(handlerName, true);
    if (!ok) {
      editor.setLoading(false);
      editor.setError('Unable to send delete request to host');
    }
  };

  return (
    <div className="space-y-6">
      {editor.error && (
        <div className="p-3 border rounded-md text-sm text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-950/20 border-red-200 dark:border-red-900 whitespace-pre-wrap">
          {editor.error}
        </div>
      )}

      <Tabs value={editor.activeTab} onValueChange={(v) => editor.setActiveTab(v as any)}>
        <TabsList>
          <TabsTrigger value="wizard">Wizard</TabsTrigger>
          <TabsTrigger value="json">Advanced JSON</TabsTrigger>
        </TabsList>

        <TabsContent value="wizard" className="space-y-4">
          <HandlerEditorWizard
            mode={mode}
            handlerName={handlerName}
            draft={editor.draft}
            setDraft={editor.setDraft}
            currentHandlerType={editor.currentHandlerType}
            handlerType={editor.handlerType}
            onChangeType={editor.onChangeType}
            http={editor.http}
            updateHttpSection={editor.updateHttpSection}
            handlerNames={editor.handlerNames}
            actionNames={editor.actionNames}
            validatorNames={editor.validatorNames}
            contextProviderNames={editor.contextProviderNames}
            registeredHandlerTypes={editor.registeredHandlerTypes}
            wizardValidation={editor.wizardValidation}
            isSaved={editor.isSaved}
            loading={editor.loading}
            deleteOpen={editor.deleteOpen}
            setDeleteOpen={editor.setDeleteOpen}
            onDelete={onDelete}
            onSave={saveFromWizard}
            onCancel={onCancel}
            onPublish={() => editor.setPublishDialogOpen(true)}
          />
        </TabsContent>

        <TabsContent value="json" className="space-y-3">
          <HandlerEditorJson
            jsonText={editor.jsonText}
            setJsonText={editor.setJsonText}
            loading={editor.loading}
            onApplyJsonToWizard={editor.applyJsonToWizard}
            onUpdateJsonFromWizard={editor.updateJsonFromWizard}
            onSave={saveFromJson}
            onCancel={onCancel}
          />
        </TabsContent>
      </Tabs>

      <PublishDialog
        open={editor.publishDialogOpen}
        handlerName={editor.draft.name ?? ''}
        handlerJson={editor.draft}
        handlerDescription={editor.draft.description}
        onOpenChange={editor.setPublishDialogOpen}
        onPublished={() => {
          // Optionally refresh or show success message
        }}
      />
    </div>
  );
}
