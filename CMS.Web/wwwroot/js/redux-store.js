/**
 * Redux-like State Management for CMS
 * A lightweight implementation of Redux patterns for ASP.NET Core Razor Pages
 */

// Core Store Implementation
class Store {
    constructor(reducer, initialState = {}, middlewares = []) {
        this.reducer = reducer;
        this.state = initialState;
        this.listeners = [];
        this.middlewares = middlewares;
        
        // Apply middlewares
        this.dispatch = this.middlewares.reduce(
            (next, middleware) => middleware(this)(next),
            this._dispatch.bind(this)
        );
    }
    
    getState() {
        return this.state;
    }
    
    _dispatch(action) {
        this.state = this.reducer(this.state, action);
        this.listeners.forEach(listener => listener(this.state));
        return action;
    }
    
    subscribe(listener) {
        this.listeners.push(listener);
        return () => {
            const index = this.listeners.indexOf(listener);
            if (index > -1) {
                this.listeners.splice(index, 1);
            }
        };
    }
}

// Utility function to combine reducers
function combineReducers(reducers) {
    return function(state = {}, action) {
        const nextState = {};
        let hasChanged = false;
        
        for (let key in reducers) {
            const reducer = reducers[key];
            const previousStateForKey = state[key];
            const nextStateForKey = reducer(previousStateForKey, action);
            nextState[key] = nextStateForKey;
            hasChanged = hasChanged || nextStateForKey !== previousStateForKey;
        }
        
        return hasChanged ? nextState : state;
    };
}

// Async middleware (Thunk-like)
function asyncMiddleware(store) {
    return function(next) {
        return function(action) {
            if (typeof action === 'function') {
                return action(store.dispatch, store.getState);
            }
            return next(action);
        };
    };
}

// Logger middleware for debugging
function loggerMiddleware(store) {
    return function(next) {
        return function(action) {
            console.group(`Action: ${action.type}`);
            console.log('Previous state:', store.getState());
            console.log('Action:', action);
            const result = next(action);
            console.log('Next state:', store.getState());
            console.groupEnd();
            return result;
        };
    };
}

// Action creators helper
function createAction(type, payloadCreator = (payload) => payload) {
    const actionCreator = function(payload) {
        return {
            type,
            payload: payloadCreator(payload)
        };
    };
    actionCreator.type = type;
    actionCreator.toString = () => type;
    return actionCreator;
}

// Async action creator helper
function createAsyncAction(prefix) {
    return {
        pending: createAction(`${prefix}/pending`),
        fulfilled: createAction(`${prefix}/fulfilled`),
        rejected: createAction(`${prefix}/rejected`)
    };
}

// Export to global scope for use in Razor Pages
window.CMS = window.CMS || {};
window.CMS.Store = Store;
window.CMS.combineReducers = combineReducers;
window.CMS.asyncMiddleware = asyncMiddleware;
window.CMS.loggerMiddleware = loggerMiddleware;
window.CMS.createAction = createAction;
window.CMS.createAsyncAction = createAsyncAction;