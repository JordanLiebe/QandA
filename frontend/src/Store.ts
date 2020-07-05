import {
  QuestionData,
  getUnansweredQuestions,
  postQuestion,
  PostQuestionData,
} from './QuestionData';
import {
  Action,
  ActionCreator,
  Dispatch,
  Reducer,
  combineReducers,
  Store,
  createStore,
  applyMiddleware,
} from 'redux';
import thunk, { ThunkAction } from 'redux-thunk';

interface QuestionsState {
  readonly loading: boolean;
  readonly unanswered: QuestionData[] | null;
  readonly postedResult?: QuestionData;
}

export interface AppState {
  readonly questions: QuestionsState;
}

interface GettingUnansweredQuestionsAction
  extends Action<'GettingUnansweredQuestions'> {}

export interface GotUnansweredQuestionsAction
  extends Action<'GotUnansweredQuestions'> {
  questions: QuestionData[];
}

export interface PostedQuestionAction extends Action<'PostedQuestion'> {
  result: QuestionData | undefined;
}

type QuestionsActions =
  | GettingUnansweredQuestionsAction
  | GotUnansweredQuestionsAction
  | PostedQuestionAction;

const initialQuestionState: QuestionsState = {
  loading: false,
  unanswered: null,
};

export const getUnansweredQuestionsActionCreator: ActionCreator<ThunkAction<
  Promise<void>,
  QuestionData[],
  null,
  GotUnansweredQuestionsAction
>> = () => {
  return async (dispatch: Dispatch) => {
    const gettingUnansweredQuestionsAction: GettingUnansweredQuestionsAction = {
      type: 'GettingUnansweredQuestions',
    };
    dispatch(gettingUnansweredQuestionsAction);
    // TODO - get the questions from server
    const questions = await getUnansweredQuestions();
    // TODO - dispatch the GotUnansweredQuestions action
    const gotUnansweredQuestionAction: GotUnansweredQuestionsAction = {
      questions,
      type: 'GotUnansweredQuestions',
    };
    dispatch(gotUnansweredQuestionAction);
  };
};

export const postQuestionActionCreator: ActionCreator<ThunkAction<
  Promise<void>,
  QuestionData,
  PostQuestionData,
  PostedQuestionAction
>> = (question: PostQuestionData) => {
  return async (dispatch: Dispatch) => {
    const result = await postQuestion(question);
    const postedQuestionAction: PostedQuestionAction = {
      type: 'PostedQuestion',
      result,
    };
    dispatch(postedQuestionAction);
  };
};

export const clearPostedQuestionActionCreator: ActionCreator<PostedQuestionAction> = () => {
  const postedQuestionAction: PostedQuestionAction = {
    type: 'PostedQuestion',
    result: undefined,
  };
  return postedQuestionAction;
};

const neverReached = (never: never) => {};

const questionsReducer: Reducer<QuestionsState, QuestionsActions> = (
  state = initialQuestionState,
  action,
) => {
  // TODO - Handle the different actions and return new state
  switch (action.type) {
    case 'GettingUnansweredQuestions': {
      // TODO - return new state
      return {
        ...state,
        unanswered: null,
        loading: true,
      };
    }
    case 'GotUnansweredQuestions': {
      // TODO - return new state
      return {
        ...state,
        unanswered: action.questions,
        loading: false,
      };
    }
    case 'PostedQuestion': {
      // TODO - return new state
      return {
        ...state,
        unanswered: action.result
          ? (state.unanswered || []).concat(action.result)
          : state.unanswered,
        postedResult: action.result,
      };
    }
    default: {
      neverReached(action);
    }
  }
  return state;
};

const rootReducer = combineReducers<AppState>({
  questions: questionsReducer,
});

export function configureStore(): Store<AppState> {
  const store = createStore(rootReducer, undefined, applyMiddleware(thunk));
  return store;
}
