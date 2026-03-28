# FOR: The `useGuardedMutation` Hook — Preventing Duplicate API Calls on Fast Clicks

> Topic: React custom hooks · React Query mutations · concurrency guards · UX patterns

---

## Step 1: Approach & Reasoning

### What was the problem?

Imagine a user sees a list of their budgets. They click "Delete" on one. The network is a bit slow. They click again. And again. Your app fires three DELETE requests to the API for the same item. The first one deletes it. The second one gets a 404. Maybe the third one accidentally deletes something else. This is a classic **double-click / rapid-click race condition**.

The old code looked like this in BudgetsPage:

```tsx
const { mutate: deleteBudget, isPending: isDeleting, variables: deletingId } = useDeleteBudget()

<button
  onClick={() => deleteBudget(budget.id, { onError: ... })}
  disabled={isDeleting && deletingId === budget.id}
>
```

This used React Query's built-in `isPending` + `variables` — but that only tracks **one** in-flight mutation at a time. If you were deleting budget A and quickly clicked budget B, `variables` would switch to B and re-enable the A button. It's not per-item — it's global.

### The solution: a custom hook that tracks in-flight IDs

The approach was to build `useGuardedMutation`, a thin wrapper that:
1. Keeps a **Set of in-flight IDs** in a `useRef`
2. Wraps `mutate` so that if the same ID is already in-flight, the call is silently dropped
3. Exposes `isPending(id)` — a per-item check

This is a **guard pattern**: wrap something potentially dangerous (a mutation that can fire many times) with a gate that only lets one instance through at a time.

---

## Step 2: Roads Not Taken

### Road 1: Keep using React Query's `variables`

The original code used `isDeleting && deletingId === budget.id`. This works for a single mutation — but React Query only remembers the **last** variables passed. If you fire delete on item A, then immediately click item B, `deletingId` becomes `"B"`, and item A's button re-enables even though A is still being deleted on the server.

**Why rejected:** broken for fast sequential clicks across different items.

### Road 2: Local `useState` per button

You could give each button its own `const [isDeleting, setIsDeleting] = useState(false)`. This is correct but verbose — you'd need to repeat the pattern in every component, every button, every mutation.

**Why rejected:** it doesn't scale. The hook approach centralizes the logic once.

### Road 3: Debounce / throttle the click handler

You could wrap the handler with `_.debounce(fn, 500)`. This delays the call slightly and collapses rapid clicks into one.

**Why rejected:** debounce introduces artificial delay. A user who clicks once would wait 500ms before anything happens. It also doesn't disable the button visually, so the user gets no feedback. The guarded mutation approach is instant on first click and gives immediate visual feedback (button dims).

### Road 4: `useReducer` to track a map of `{id: boolean}`

You could maintain a `Map<string, boolean>` in component state. This works but triggers re-renders every time an item enters or leaves the in-flight state, across the entire component tree.

**Why rejected:** `useRef` doesn't trigger re-renders when mutated. Since `isPending(id)` is only called in render anyway (and React Query's own re-render cycle covers the visual update), `useRef` is the right tool here.

---

## Step 3: How the Parts Connect

Here's the full chain, from user click to guarded execution:

```
User clicks "Delete" button
        ↓
onClick calls guardedDelete(id, { onError: ... })
        ↓
useGuardedMutation checks: is `id` in inFlight Set?
  → YES: return early — no-op, nothing fires
  → NO:  add id to inFlight Set, call real mutate()
        ↓
React Query fires the DELETE HTTP request
        ↓
onSettled fires (success or error)
        ↓
id is removed from inFlight Set
        ↓
button re-enables (next render, isPending(id) = false)
```

The `disabled={isDeleting(id)}` on the button is the **visual gate** — it dims the button and prevents click events. But even without it, the guard in the hook itself would stop duplicate calls. That's defense in depth: two layers of protection.

**What breaks if you remove `onSettled` cleanup?**
The ID stays in the Set forever. The button never re-enables. The user can never delete that item again until they reload.

**What breaks if you remove `disabled={isDeleting(id)}`?**
The visual feedback disappears, but the hook still guards correctly. Functionally safe, but bad UX — the button doesn't look inactive during a delete.

---

## Step 4: Tools, Methods & Frameworks

### `useRef<Set<string>>`

A `ref` in React is a mutable container that **doesn't trigger re-renders** when you change it. This is perfect for tracking in-flight state: you don't need the component to re-render just because an ID entered the Set. The re-render already happens because React Query's own mutation state changes.

**Contrast with `useState`:** `useState` would cause an extra re-render on every add/delete to the set. `useRef` avoids that churn.

### `useCallback`

Both `guarded` and `isPending` are wrapped in `useCallback`. Without this, a new function reference would be created on every render, breaking memoization in child components and potentially causing infinite loops in `useEffect` dependencies.

**The dependency array:** `guarded` depends on `[mutate]`, because the underlying mutate function is what it calls. `isPending` has `[]` — it never needs to change because it only reads from `inFlight.current` which is always the same ref object.

### React Query's `mutate` function

React Query's `mutate` accepts `onSuccess`, `onError`, and `onSettled` callbacks as a second argument. The hook wraps `onSettled` to inject the cleanup logic, then forwards the rest of the user's options unchanged. This is called **callback interception** — you piggyback on an existing callback hook.

---

## Step 5: Tradeoffs

| What you gain | What you give up |
|---|---|
| Per-item pending tracking (correct for lists) | Slightly more indirection — you call `guarded()` not `mutate()` |
| No duplicate API calls on rapid click | Hook only works with mutations where the variable is a `string` ID |
| Reusable across all pages (Budgets, Trades, Transactions, Watchlist) | `isPending(id)` doesn't trigger a re-render on its own — you rely on React Query's render cycle |
| Zero artificial delay vs debounce | N/A |

**The `TVariables extends string` constraint** is a real limitation. This hook only works when your mutation variable is a plain string ID. If your mutation takes a full object (e.g. `{ id, reason }`), you'd need to adapt the hook to extract a key. For this codebase, all deletes are `(id: string) => void`, so the constraint is fine.

---

## Step 6: Mistakes & Dead Ends

### The `variables` trap

The original `BudgetsPage` used React Query's `variables` to track which item was deleting:

```tsx
disabled={isDeleting && deletingId === budget.id}
```

This looks correct at first glance. And for a single item it is. But `variables` is a **last-write-wins** value — it tracks the most recent mutation call. In a list with many delete buttons, this breaks as soon as you fire more than one delete.

The mistake here is subtle: React Query's `variables` is designed for "what did I just submit?", not "which of my many items is currently being processed?" It's a scalar, not a map.

### Could `isPending(id)` silently fail to re-render?

Yes, this is the most subtle potential footgun. `isPending` reads from `inFlight.current` which is a `useRef`. Mutating a ref **does not cause a re-render**. So if `isPending(id)` returned `false` before a mutation and you called `guarded(id)`, the button wouldn't visually update... unless something else triggered a render.

In practice, something always does: React Query transitions its own `isPending` state when the mutation fires, which causes the component to re-render. At that point, `isPending(id)` reads the current ref value (now `true`) and the button dims correctly.

This is an **implicit coupling** — the hook works correctly because React Query's state machine triggers renders at the right moments. If you used `useGuardedMutation` with a plain `fetch()` instead of a React Query mutation, the button would never dim.

---

## Step 7: Future Pitfalls

**1. Don't use this hook with non-string variables without adapting it.**
If your mutation is `deleteBudget({ id, month })`, the hook won't work — it expects a plain string. You'd need to extract the key: `const key = id` and track by that.

**2. If you swap React Query for something else, the re-render guarantee disappears.**
The hook relies on React Query triggering component re-renders when mutations fire. If you migrate to SWR or plain `fetch`, you need to track re-renders differently.

**3. The guard resets on component unmount.**
If the component unmounts while a mutation is in flight, the ref is gone. When the component remounts, the Set is empty again. This is almost always fine, but if your mutation's `onSettled` tries to update state on an unmounted component, you'll get a warning. React Query handles this gracefully (it won't call `onSettled` after unmount), but keep this in mind if you use this pattern outside React Query.

**4. "Looks disabled but isn't" — accessibility.**
Setting `disabled` on a button prevents clicks. But screen readers may still announce the button as available if you only use CSS (`disabled:opacity-50`) without the HTML `disabled` attribute. This code uses the real `disabled` attribute, which is correct.

---

## Step 8: Expert vs. Beginner Lens

A **beginner** sees this change and thinks: "We added a hook that stops double-clicking. That's nice."

An **expert** notices:

1. **The ref-vs-state choice is deliberate and load-bearing.** Using `useState` here would work but add unnecessary renders. The author chose `useRef` specifically to avoid that — which shows understanding of React's rendering model, not just its API.

2. **The `onSettled` interception pattern is elegant.** Instead of requiring the caller to manually call a cleanup function, the hook wraps `onSettled` transparently. The caller's own `onSettled` still fires (via `options?.onSettled?.(...)`) — it's additive, not replacing. This is the **Open/Closed Principle** applied to a hook: open for extension (you can still pass your own onSettled), closed for modification (the guard logic is encapsulated).

3. **The hook is stateless across renders but stateful across time.** The ref persists between renders but resets when the component unmounts. This is intentional — you don't want a guard that leaks across component lifecycles.

4. **The generic constraint `TVariables extends string`** is a deliberate narrowing. It makes the hook simpler and the type signature honest — if your variable isn't a string, TypeScript will tell you, and you'll know to adapt the hook.

5. **Defense in depth.** The button's `disabled` attribute stops clicks at the UI layer. The `if (inFlight.current.has(id)) return` stops them at the logic layer. Two independent guards. Even if someone removed the `disabled` attribute via browser devtools, the hook still prevents the duplicate call.

---

## Step 9: Transferable Lessons

### Lesson 1: "A ref is a variable that survives renders without causing them"

This is one of React's most important mental models. `useState` = "I want the UI to react to this value changing." `useRef` = "I want to remember this value, but changing it isn't a UI event." Use refs for timers, in-flight trackers, previous values, DOM references — anything where mutation is a side effect, not a cause.

**Analogy:** `useState` is a whiteboard in the conference room — when you change it, everyone in the room reacts. `useRef` is a sticky note in your pocket — you can update it silently.

### Lesson 2: "Intercept callbacks, don't replace them"

When you need to add behavior to a callback without breaking the original, wrap it:

```ts
onSettled(data, error, variables, context) {
  inFlight.current.delete(id)          // your new behavior
  options?.onSettled?.(...)            // original behavior still fires
}
```

This is the same pattern used in middleware, decorators, and AOP (Aspect-Oriented Programming). You're adding a cross-cutting concern (cleanup) without touching the business logic.

### Lesson 3: "Track by ID, not by global boolean"

`isLoading = true` tells you *something* is loading. `isLoading(id) = true` tells you *which thing* is loading. In any list UI, you almost always want the second form. A global boolean is the wrong abstraction the moment you have more than one item.

**Applies to:** loading spinners, disabled states, optimistic updates, selection state.

### Lesson 4: "The simplest abstraction that prevents repetition"

This hook is 47 lines. It's used in 4 places. Without it, each of those 4 places would need its own ref, its own guard logic, its own cleanup. The hook extracts the pattern once and names it clearly. This is the essence of good abstraction: not "make it clever", but "make it say what it does and do what it says."

### Lesson 5: "Two layers of defense are better than one"

`disabled` + hook guard = belt and suspenders. Neither is sufficient alone for all attack vectors (a `disabled` attribute can be bypassed via devtools; a hook guard without `disabled` gives no visual feedback). Together, they're robust against both accidental user behavior and deliberate tampering.

This principle — defense in depth — comes from security but applies everywhere: validation on both frontend and backend, retries with circuit breakers, optimistic UI with server confirmation.

---

*This teaching doc covers the `useGuardedMutation` hook introduced in the current branch, applied across BudgetsPage, TradesPage, TransactionsPage, and WatchlistManager.*
