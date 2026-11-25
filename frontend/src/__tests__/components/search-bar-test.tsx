import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react-native';
import SearchBar from '@/components/searchBar';

describe('<SearchBar />', () => {


  test('calls onSubmitSearch with the query when submitted', async () => {
    const onSubmitSearch = jest.fn();
    await render(<SearchBar onSubmitSearch={onSubmitSearch} />);

    const input = screen.getByPlaceholderText('Search...');
    await fireEvent.changeText(input, 'batman');
    await fireEvent(input, 'submitEditing');

    expect(onSubmitSearch).toHaveBeenCalledWith('batman');
    expect(onSubmitSearch).toHaveBeenCalledTimes(1);
  });



  test('does not call onSubmitSearch when the query is empty', async () => {
    const onSubmitSearch = jest.fn();
    await render(<SearchBar onSubmitSearch={onSubmitSearch} />);

    const input = screen.getByPlaceholderText('Search...');
    await fireEvent(input, 'submitEditing');

    expect(onSubmitSearch).not.toHaveBeenCalled();
  });


});
