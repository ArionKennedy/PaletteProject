import { render } from '@testing-library/react-native';
import { useAuth } from '@clerk/expo';
import IndexRoute from '@/app/index';

//mocking authentication
jest.mock('@clerk/expo', () => ({                                                                                      
    useAuth: jest.fn(),
})); 